namespace EcoguardPoller.Models
{
    internal class EcoGuardDeviceResult
    {
        public int ID { get; set; }
        public string Name { get; set; } = "";
        public List<EcoGuardFunctionResult> Result { get; set; } = new();

    }
    public class EcoGuardFunctionResult
    {
        public string Utl { get; set; } = "";
        public string Func { get; set; } = "";
        public string Unit { get; set; } = "";
        public List<EcoGuardValue> Values { get; set; } = new();
    }
    public class EcoGuardValue
    {
        public long Time { get; set; }
        public double Value { get; set; }
    }

}
