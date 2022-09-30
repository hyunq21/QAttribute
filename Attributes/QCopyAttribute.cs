namespace System
{
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class QCopyAttribute : System.Attribute
    {
    }
    public interface ICopyable
    {
        public void Copy(object value);
    }
}
