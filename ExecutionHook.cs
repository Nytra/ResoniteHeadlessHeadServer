using FrooxEngine;
using System.Reflection.Metadata;
using System.Reflection;
using Elements.Core;

[assembly: DataModelAssembly(DataModelAssemblyType.Dependency)]

namespace Thundagun;

[ImplementableClass(true)]
internal static class ExecutionHook
{
	// Fields for reflective access
	private static Type? __connectorType;
	private static Type? __connectorTypes;

	// Static constructor for initializing the hook
	static ExecutionHook()
	{
		UniLog.Log("Start of ExecutionHook!");
		Thundagun.Setup(new string[] { });
	}

	// Method to instantiate the connector
	private static DummyConnector InstantiateConnector() => new DummyConnector();

	// Dummy connector class implementing IConnector
	private class DummyConnector : IConnector
	{
		public IImplementable? Owner { get; private set; }

		public void ApplyChanges() { }

		public void AssignOwner(IImplementable owner) => Owner = owner;

		public void Destroy(bool destroyingWorld) { }

		public void Initialize() { }

		public void RemoveOwner() => Owner = null;
	}
}