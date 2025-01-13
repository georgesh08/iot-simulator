namespace DataSimulator;

internal class Program
{
	static void Main(string[] args)
	{
		if (args.Length < 2)
		{
			Console.WriteLine("Invalid number of arguments. Should be two: <number of devices> <data send period>");
			return;
		}
	}
}
