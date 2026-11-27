#if LEGATO_BSML_ALIASED
extern alias Bsml;
#endif

#nullable enable

#if LEGATO_BSML_ALIASED
using Bsml::BeatSaberMarkupLanguage.Components;
#else
using BeatSaberMarkupLanguage.Components;
#endif
using HMUI;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
#if LEGATO_EARLY_VR_UI
using LevelListTableCell = global::StandardLevelListTableCell;
#endif
#if LEGATO_BSML_ALIASED
using static Bsml::BeatSaberMarkupLanguage.Components.CustomListTableData;
#else
using static BeatSaberMarkupLanguage.Components.CustomListTableData;
#endif

namespace Legato {
    internal static class CustomListTableDataExtensions {
        private static readonly FieldInfo SongListTableCellPrefab = typeof(CustomListTableData).GetField(
#if !LEGATO_BSML_CURRENT && (BEAT_SABER_1_29_0 || BEAT_SABER_1_37_1)
            "songListTableCellInstance",
#else
            "songListTableCellPrefab",
#endif
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(typeof(CustomListTableData).FullName, "songListTableCellPrefab");

        internal static IList<CustomCellInfo> GetData(this CustomListTableData tableData) {
#if !LEGATO_BSML_CURRENT && (BEAT_SABER_1_29_0 || BEAT_SABER_1_37_1)
            return tableData.data;
#else
            return tableData.Data;
#endif
        }

        internal static TableView GetTableView(this CustomListTableData tableData) {
#if !LEGATO_BSML_CURRENT && (BEAT_SABER_1_29_0 || BEAT_SABER_1_37_1)
            return tableData.tableView;
#else
            return tableData.TableView;
#endif
        }

        internal static Sprite GetIcon(this CustomCellInfo cell) {
#if !LEGATO_BSML_CURRENT && (BEAT_SABER_1_29_0 || BEAT_SABER_1_37_1)
            return cell.icon;
#else
            return cell.Icon;
#endif
        }

        internal static LevelListTableCell? GetSongListTableCellPrefab(this CustomListTableData tableData) {
            return SongListTableCellPrefab.GetValue(tableData) as LevelListTableCell;
        }

        internal static void SetSongListTableCellPrefab(this CustomListTableData tableData, LevelListTableCell tableCell) {
            SongListTableCellPrefab.SetValue(tableData, tableCell);
        }
    }
}
