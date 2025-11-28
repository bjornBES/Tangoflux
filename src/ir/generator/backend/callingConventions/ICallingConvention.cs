public interface ICallingConvention
{
    public int StackAlignment { get; set; }
    RegisterInfo GetRegister(RegisterFunction function);
    string GetRegisterName(RegisterInfo register);
}
