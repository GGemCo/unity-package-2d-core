namespace GGemCo2DCore
{
    public interface IItemUseAction
    {
        ResultCommon CanExecute(ItemUseContext ctx);
        ResultCommon Execute(ItemUseContext ctx);
    }
}
