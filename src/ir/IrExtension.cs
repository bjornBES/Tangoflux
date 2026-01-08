
public static class IrExtension
{
    public static bool GetOperand<T>(this IrOperand operand, out T type) where T : IrOperand
    {
        if (operand is T data)
        {
            type = data;
            return true;
        }
        type = null;
        return false;
    }
}