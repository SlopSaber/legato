#if LEGATO_EARLY_VR_UI
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HMUI;

public sealed class SimpleTextDropdown : MonoBehaviour, TableView.IDataSource
{
    private const string ReuseId = "BSMLDropdown";
    private List<string> choices = new();
    private Func<TableView, EarlyDropdownCell> createCell;
    private Button button;
    private TextMeshProUGUI valueText;
    private ModalView modal;
    private TableView table;
    private int selectedIndex = -1;
    private bool interactable = true;

    public event Action<SimpleTextDropdown, int> didSelectCellWithIdxEvent;

    public bool Interactable
    {
        get => interactable;
        set
        {
            interactable = value;
            RefreshButton();
            if (!interactable)
            {
                Close();
            }
        }
    }

    internal void Bind(Button toggle, TextMeshProUGUI text, ModalView listModal, TableView list,
        Func<TableView, EarlyDropdownCell> cellFactory)
    {
        button = toggle;
        valueText = text;
        modal = listModal;
        table = list;
        createCell = cellFactory;
        button.onClick.AddListener(Open);
        modal.blockerClickedEvent += Close;
        table.didSelectRowEvent += OnSelectRow;
        RefreshButton();
    }

    public void SetTexts(IEnumerable<string> texts)
    {
        choices = new List<string>(texts);
        selectedIndex = choices.Count > 0 ? Mathf.Clamp(selectedIndex, 0, choices.Count - 1) : -1;
        RefreshButton();
        if (choices.Count == 0)
        {
            SetValueText(null);
            Close();
        }
        else
        {
            SetValueText(choices[selectedIndex]);
            if (modal.gameObject.activeSelf)
            {
                table.ReloadData();
                SelectCellWithIdx(selectedIndex);
            }
        }
    }

    public void SelectCellWithIdx(int index)
    {
        if (index < 0 || index >= choices.Count)
        {
            return;
        }

        selectedIndex = index;
        if (modal.gameObject.activeSelf)
        {
            table.SelectRow(index, false);
        }

        SetValueText(choices[index]);
    }

    public void SetValueText(string value) => valueText.text = value ?? string.Empty;

    float TableView.IDataSource.RowHeight() => 6f;

    int TableView.IDataSource.NumberOfRows() => choices.Count;

    TableCell TableView.IDataSource.CellForRow(int row)
    {
        var cell = (EarlyDropdownCell)table.DequeueReusableCellForIdentifier(ReuseId);
        if (cell == null)
        {
            cell = createCell(table);
            cell.reuseIdentifier = ReuseId;
            cell.ConfirmSelection = SelectFromInput;
        }

        cell.text = choices[row] ?? string.Empty;
        return cell;
    }

    private void Open()
    {
        if (!interactable || choices.Count == 0)
        {
            return;
        }

        modal.Show(true);
        table.ReloadData();
        SelectCellWithIdx(selectedIndex);
        table.ScrollToRow(selectedIndex, false);
    }

    private void RefreshButton()
    {
        if (button != null)
        {
            button.interactable = interactable && choices.Count > 0;
        }
    }

    private void Close()
    {
        if (modal != null)
        {
            modal.Hide(false);
        }
    }

    private void OnSelectRow(TableView _, int row) => SelectFromInput(row);

    private void SelectFromInput(int row)
    {
        if (!interactable || row < 0 || row >= choices.Count)
        {
            return;
        }

        selectedIndex = row;
        SetValueText(choices[row]);
        Close();
        didSelectCellWithIdxEvent?.Invoke(this, row);
    }

    private void OnDisable() => Close();
}

internal sealed class EarlyDropdownCell : SimpleListTableCell
{
    internal Action<int> ConfirmSelection { get; set; }

    public override void OnPointerClick(PointerEventData eventData)
    {
        if (selected && eventData.button == PointerEventData.InputButton.Left)
        {
            ConfirmSelection?.Invoke(row);
            return;
        }

        base.OnPointerClick(eventData);
    }

    public override void OnSubmit(BaseEventData eventData)
    {
        if (selected)
        {
            ConfirmSelection?.Invoke(row);
            return;
        }

        base.OnSubmit(eventData);
    }
}
#endif
