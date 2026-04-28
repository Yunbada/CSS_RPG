namespace CSS_RPG.UI
{
    public interface IInventoryUIState
    {
        void Enter();
        void HandleInput(int key);
        void RefreshDisplay();
        void Exit();
    }
}
