using System.Collections.Generic;
using System.Threading.Tasks;

public interface IAgent
{
	string Name { get; }
	Task<string> ExecuteAsync(string task, Dictionary<string, object> arguments);
}