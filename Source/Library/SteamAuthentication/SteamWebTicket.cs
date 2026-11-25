using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Steamworks;
#if BEAT_SABER_1_29_0
using UnityEngine;
#endif

namespace Legato.Platform {
    internal sealed class SteamWebTicket : IDisposable {
        private HAuthTicket _ticket = HAuthTicket.Invalid;
        internal string Token { get; private set; } = string.Empty;

        internal static async Task<SteamWebTicket> Create(string identity, TimeSpan timeout, CancellationToken cancellationToken = default) {
            cancellationToken.ThrowIfCancellationRequested();
            var result = new SteamWebTicket();
            var completion = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
#if BEAT_SABER_1_29_0
            using var callback = Callback<WebTicketResponse>.Create(response => {
                if (response.Ticket != result._ticket) return;
                Complete(completion, response.Result, response.Size, response.Bytes);
            });
#else
            using var callback = Callback<GetTicketForWebApiResponse_t>.Create(response => {
                if (response.m_hAuthTicket != result._ticket) return;
                Complete(completion, response.m_eResult, response.m_cubTicket, response.m_rgubTicket);
            });
#endif
            try {
#if BEAT_SABER_1_29_0
                if (IntPtr.Size != 8 || Environment.OSVersion.Platform != PlatformID.Win32NT)
                    throw new PlatformNotSupportedException("Steam web tickets require Windows x64 for this profile.");
                IntPtr user = SteamClient.GetISteamUser(SteamAPI.GetHSteamUser(), SteamAPI.GetHSteamPipe(), "SteamUser023");
                if (user == IntPtr.Zero) throw new NotSupportedException("SteamUser023 is unavailable from the running Steam client.");
                // SteamUser023 places GetAuthTicketForWebApi at vtable slot 14.
                IntPtr entry = Marshal.ReadIntPtr(Marshal.ReadIntPtr(user), 14 * IntPtr.Size);
                if (entry == IntPtr.Zero) throw new NotSupportedException("The Steam web ticket entry point is unavailable.");
                var request = Marshal.GetDelegateForFunctionPointer<RequestWebTicket>(entry);
                result._ticket = new HAuthTicket(request(user, identity));
#else
                result._ticket = SteamUser.GetAuthTicketForWebApi(identity);
#endif
                if (result._ticket == HAuthTicket.Invalid) throw new InvalidOperationException("Steam did not issue a web authentication ticket.");
#if BEAT_SABER_1_29_0
                Application.quitting += result.Dispose;
#endif
                using var timeoutCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var completed = await Task.WhenAny(completion.Task, Task.Delay(timeout, timeoutCancellation.Token)).ConfigureAwait(true);
                cancellationToken.ThrowIfCancellationRequested();
                if (completed != completion.Task)
                    throw new TimeoutException("Steam authentication ticket timed out.");
                timeoutCancellation.Cancel();
                result.Token = await completion.Task.ConfigureAwait(true);
                return result;
            } catch {
                result.Dispose();
                throw;
            }
        }

        private static void Complete(TaskCompletionSource<string> completion, EResult result, int size, byte[] bytes) {
            if (result != EResult.k_EResultOK || bytes == null || size <= 0 || size > bytes.Length) {
                completion.TrySetException(new InvalidOperationException("Steam ticket request failed: " + result));
                return;
            }
            completion.TrySetResult(BitConverter.ToString(bytes, 0, size).Replace("-", string.Empty));
        }

        public void Dispose() {
            var ticket = _ticket;
            if (ticket == HAuthTicket.Invalid) return;
            _ticket = HAuthTicket.Invalid;
#if BEAT_SABER_1_29_0
            Application.quitting -= Dispose;
            // Steamworks.NET keeps its cached interface after the game's SteamManager shuts down.
            var steamManager = SteamManager.s_instance;
            if (steamManager == null || !steamManager.m_bInitialized) return;
#endif
            try {
                SteamUser.CancelAuthTicket(ticket);
            } catch (InvalidOperationException exception) when (string.Equals(exception.Message, "Steamworks is not initialized.", StringComparison.Ordinal)) {
                // Steam invalidates outstanding tickets before its shutdown callback.
            }
        }

#if BEAT_SABER_1_29_0
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate uint RequestWebTicket(IntPtr user, [MarshalAs(UnmanagedType.LPUTF8Str)] string identity);

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        [CallbackIdentity(168)]
        private struct WebTicketResponse {
            public HAuthTicket Ticket;
            public EResult Result;
            public int Size;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2560)]
            public byte[] Bytes;
        }
#endif
    }
}
