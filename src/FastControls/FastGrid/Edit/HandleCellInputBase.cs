namespace OpenSilver.ControlsKit.FastGrid.Edit
{
    internal interface HandleCellInputBase {
        void Subscribe();
        void Unsubscribe();

        void GotFocus(bool viaClick);
        void LostFocus();
    }
}
