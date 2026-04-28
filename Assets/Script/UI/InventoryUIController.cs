using UnityEngine;
using System.Collections.Generic;
using CSS_RPG.UI;

public enum InventoryViewState
{
    Main,
    Equipment,
    Inventory,
    Crafting,
    Debug,
    TradeSearch,
    TradeSession,
    TradeInventorySelect
}

public class InventoryUIController
{
    public InventoryViewState CurrentStateEnum { get; private set; } = InventoryViewState.Main;

    public InventorySystem Inventory { get; private set; }
    public EquipmentSystem Equipment { get; private set; }
    public PlayerClass PlayerClass { get; private set; }
    public PlayerExperience PlayerExp { get; private set; }
    public PlayerTradeSystem Trade { get; private set; }

    public InventoryView View { get; private set; }

    private Dictionary<InventoryViewState, IInventoryUIState> states;
    private IInventoryUIState currentState;

    public InventoryUIController(InventorySystem inv, EquipmentSystem equip, PlayerClass pClass, PlayerExperience pExp, PlayerTradeSystem pTrade)
    {
        Inventory = inv;
        Equipment = equip;
        PlayerClass = pClass;
        PlayerExp = pExp;
        Trade = pTrade;

        View = new InventoryView();

        InitializeStates();
    }

    private void InitializeStates()
    {
        states = new Dictionary<InventoryViewState, IInventoryUIState>
        {
            { InventoryViewState.Main, new MainUIState(this) },
            { InventoryViewState.Equipment, new EquipmentUIState(this) },
            { InventoryViewState.Inventory, new InventoryUIState(this) },
            { InventoryViewState.Crafting, new CraftingUIState(this) },
            { InventoryViewState.Debug, new DebugUIState(this) },
            { InventoryViewState.TradeSearch, new TradeSearchUIState(this) },
            { InventoryViewState.TradeSession, new TradeSessionUIState(this) },
            { InventoryViewState.TradeInventorySelect, new TradeInventorySelectUIState(this) }
        };

        ChangeState(InventoryViewState.Main);
    }

    public void ChangeState(InventoryViewState newState)
    {
        if (currentState != null)
        {
            currentState.Exit();
        }

        CurrentStateEnum = newState;
        currentState = states[newState];
        currentState.Enter();
        RefreshDisplay();
    }

    public void TogglePanel(bool isVisible)
    {
        View.TogglePanel(isVisible);
    }

    public void DestroyUI()
    {
        View.DestroyUI();
    }

    public void ResetToMain()
    {
        ChangeState(InventoryViewState.Main);
    }

    public void HandleInput(int pressedKey)
    {
        if (currentState != null)
        {
            currentState.HandleInput(pressedKey);
        }
    }

    public void RefreshDisplay()
    {
        if (currentState != null)
        {
            currentState.RefreshDisplay();
        }
    }
}
