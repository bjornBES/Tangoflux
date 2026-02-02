public abstract class AsmOperand
{
    public abstract string GetByteRepresentation();
    public abstract string GetLongRepresentation();

    public bool IsType<T>(out T result) where T : AsmOperand
    {
        if (this is T)
        {
            result = this as T;
            return true;
        }
        result = default;
        return false;
    }
}
