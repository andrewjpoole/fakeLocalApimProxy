namespace FakeLocalApimProxy
{
    public class Redirection 
    {
        public string Match { get; init; } = "";
        public string Uri { get; init; } = "";

        public static Redirection Empty() => new Redirection();
    }
}
