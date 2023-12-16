using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Text.RegularExpressions;

using logPrint.Ansi;
using logPrint.Config.Flags.Evaluator;
using logPrint.Utils;

namespace logPrint.Config.Flags;

internal sealed class Flag : NamedElement
{
	const string RESET = "#!#~!~";


	static readonly SafeDictionary<string, string> TypeMap = new(
		(key, dict) => key.EndsWith("?", StringComparison.Ordinal)
			? $"Nullable`1[{FindType(dict[key.Substring(0, key.Length - 1)]).FullName}]"
			: dict.ContainsKey(key)
				? dict[key]
				: key
	) {
		{ "bool", nameof(Boolean) },
		{ "byte", nameof(Byte) },
		{ "sbyte", nameof(SByte) },
		{ "ushort", nameof(UInt16) },
		{ "short", nameof(Int16) },
		{ "uint", nameof(UInt32) },
		{ "int", nameof(Int32) },
		{ "ulong", nameof(UInt64) },
		{ "float", nameof(Single) },
		{ "double", nameof(Double) },
		{ "string", nameof(String) }
	};


	Regex _offRE;
	Regex _onRE;
	Regex _transitionToOffRE;
	Regex _transitionToOnRE;
	Regex _toggleRE;
	string[] _outputLookup;
	FlagState _state;
	Dictionary<string, Type> _types;
	List<Define> _defines;
	List<Field> _consts;
	List<Field> _fields;
	List<Property> _properties;
	List<Method> _methods;
	List<Eval> _evals;
	string _lastEvalOutput = " ";


	public event StateChangeCallback OnStateChange;


	internal FlagState State {
		get => _state;
		private set {
			if (!Enum.IsDefined(typeof(FlagState), value)) {
				throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(FlagState));
			}


			if (_state != value || _state == InitialState && Types != null) {
				OnStateChange?.Invoke(this, value);
			}

			_state = value;

			if (_state == InitialState && Types != null) {
				_lastEvalOutput = EvalsList.FirstOrDefault(eval => eval.When == "Else")?.Output ?? "~k~ ";
			}
		}
	}


	[ConfigurationProperty("off", IsRequired = false)]
	string Off => this["off"] as string;

	[ConfigurationProperty("on", IsRequired = false)]
	string On => this["on"] as string;


	[ConfigurationProperty("transitionToOff", IsRequired = false)]
	string TransitionToOff => this["transitionToOff"] as string;

	[ConfigurationProperty("transitionToOn", IsRequired = false)]
	string TransitionToOn => this["transitionToOn"] as string;


	[ConfigurationProperty("offMatch", IsRequired = false)]
	string OffMatchStr => this["offMatch"] as string;

	[ConfigurationProperty("onMatch", IsRequired = false)]
	string OnMatchStr => this["onMatch"] as string;


	[ConfigurationProperty("transitionToOffMatch", IsRequired = false)]
	string TransitionToOffMatchStr => this["transitionToOffMatch"] as string;

	[ConfigurationProperty("transitionToOnMatch", IsRequired = false)]
	string TransitionToOnMatchStr => this["transitionToOnMatch"] as string;


	[ConfigurationProperty("toggleMatch", IsRequired = false)]
	string ToggleMatchStr => this["toggleMatch"] as string;


	[ConfigurationProperty("initialState", IsRequired = false, DefaultValue = FlagState.Off)]
	public FlagState InitialState => (FlagState)this["initialState"];


	[ConfigurationProperty("types", IsRequired = false)]
	internal string TypesStr => this["types"] as string;


	[ConfigurationProperty("defines", IsDefaultCollection = false)]
	[ConfigurationCollection(typeof(GenericCollection<Define>), AddItemName = "define")]
	GenericCollection<Define> Defines => this["defines"] as GenericCollection<Define>;

	[ConfigurationProperty("consts", IsDefaultCollection = false)]
	[ConfigurationCollection(typeof(GenericCollection<Field>), AddItemName = "const")]
	GenericCollection<Field> Consts => this["consts"] as GenericCollection<Field>;

	[ConfigurationProperty("fields", IsDefaultCollection = false)]
	[ConfigurationCollection(typeof(GenericCollection<Field>), AddItemName = "field")]
	GenericCollection<Field> Fields => this["fields"] as GenericCollection<Field>;

	[ConfigurationProperty("properties", IsDefaultCollection = false)]
	[ConfigurationCollection(typeof(GenericCollection<Property>), AddItemName = "property")]
	GenericCollection<Property> Props => this["properties"] as GenericCollection<Property>;

	[ConfigurationProperty("methods", IsDefaultCollection = false)]
	[ConfigurationCollection(typeof(GenericCollection<Method>), AddItemName = "method")]
	GenericCollection<Method> Methods => this["methods"] as GenericCollection<Method>;

	[ConfigurationProperty("", IsDefaultCollection = true)]
	[ConfigurationCollection(typeof(EvalCollection), AddItemName = "eval")]
	EvalCollection Evals => this[""] as EvalCollection;

	internal Dictionary<string, Type> Types
		=> _types ??= string.IsNullOrEmpty(TypesStr)
			? null
			: ParseTypes(TypesStr);

	public List<Define> DefinesList => _defines ??= Defines.ToList();
	public List<Field> ConstsList => _consts ??= Consts.ToList();
	public List<Field> FieldsList => _fields ??= Fields.ToList();
	public List<Property> PropertiesList => _properties ??= Props.ToList();
	public List<Method> MethodsList => _methods ??= Methods.ToList();
	public List<Eval> EvalsList => _evals ??= Evals.ToList();


	public bool IsQuerying => OnStateChange != null;


	Regex OffRE
		=> _offRE ??= string.IsNullOrEmpty(OffMatchStr)
			? null
			: new Regex(OffMatchStr);

	internal Regex OnRE
		=> _onRE ??= string.IsNullOrEmpty(OnMatchStr)
			? null
			: new Regex(OnMatchStr);

	Regex TransitionToOffRE
		=> _transitionToOffRE ??= string.IsNullOrEmpty(TransitionToOffMatchStr)
			? null
			: new Regex(TransitionToOffMatchStr);

	internal Regex TransitionToOnRE
		=> _transitionToOnRE ??= string.IsNullOrEmpty(TransitionToOnMatchStr)
			? null
			: new Regex(TransitionToOnMatchStr);

	Regex ToggleRE
		=> _toggleRE ??= string.IsNullOrEmpty(ToggleMatchStr)
			? null
			: new Regex(ToggleMatchStr);


	public List<string> SelectedDefines { get; set; }


	static Dictionary<string, Type> ParseTypes(string input)
	{
		return input.Split(';')
			.Select(part => part.Split('='))
			.Select(array => new[] { array[0], TypeMap[array[1]] })
			.Select(
				array => new {
					key = array[0],
					type = FindType(array[1])
				}
			)
			.ToDictionary(pair => pair.key, pair => pair.type);
	}

	static Type FindType(string typeName)
	{
		return AppDomain.CurrentDomain
			.GetAssemblies()
			.Select(
				assembly => new {
					assembly,
					namespaces = assembly
						.GetTypes()
						.Select(t => t.Namespace + ".")
						.Distinct()
				}
			)
			.SelectMany(x => x.namespaces.Select(ns => x.assembly.GetType(ns + typeName)))
			.First(type => type != null);
	}


	protected override void ListErrors(IList errorList)
	{
		base.ListErrors(errorList);

		if (string.IsNullOrEmpty(TypesStr)) {
			if (string.IsNullOrEmpty(Off)) {
				errorList.Add("Off must be populated!");
			}

			if (string.IsNullOrEmpty(On)) {
				errorList.Add("On must be populated!");
			}

			_outputLookup = new[] {
				Off + RESET,
				TransitionToOff + RESET,
				TransitionToOn + RESET,
				On + RESET
			};
		} else {
			if (string.IsNullOrEmpty(OnMatchStr)) {
				errorList.Add("OnMatch must be specified for an eval-flag!");
			}

			if (EvalsList.Count < 1) {
				errorList.Add("Evals must be specified for an eval-flag!");
			}
		}

		State = InitialState;
	}


	public string Process(string line)
	{
		if (Types != null) {
			return ProcessEval(line) + RESET;
		}


		if (OffRE?.IsMatch(line) ?? false) {
			State = FlagState.Off;
		}

		if (OnRE?.IsMatch(line) ?? false) {
			State = FlagState.On;
		}

		if (TransitionToOffRE?.IsMatch(line) ?? false) {
			State = FlagState.TransitioningOff;
		}

		if (TransitionToOnRE?.IsMatch(line) ?? false) {
			State = FlagState.TransitioningOn;
		}

		if (ToggleRE?.IsMatch(line) ?? false) {
			// ReSharper disable once BitwiseOperatorOnEnumWithoutFlags	- this does what I want.
			State ^= FlagState.On;
		}

		return _outputLookup[(int)State];
	}

	string ProcessEval(string line)
	{
		var newOutput = EvalsList
			.Select(eval => eval.Process(line, this))
			.ToList()		// Ensure all evals get all the data.
			.FirstOrDefault(output => output != null);

		if (newOutput == null || newOutput == _lastEvalOutput) {
			return _lastEvalOutput;
		}


		_lastEvalOutput = newOutput;
		OnStateChange?.Invoke(this, newFlagState: default);

		return _lastEvalOutput;
	}


	public string Reset()
	{
		State = InitialState;

		if (Types != null) {
			ResetEval();
		}

		return new string('-', (_lastEvalOutput ?? "-").StripColourCodes().Length);
	}

	void ResetEval()
	{
		EvalsList.ForEach(eval => eval.Reset(this));
	}


	public Flag Copy()
	{
		return new() {
			["name"] = Name,
			["off"] = Off,
			["offMatch"] = OffMatchStr,
			["transitionToOff"] = TransitionToOff,
			["transitionToOffMatch"] = TransitionToOffMatchStr,
			["transitionToOn"] = TransitionToOn,
			["transitionToOnMatch"] = TransitionToOnMatchStr,
			["on"] = On,
			["onMatch"] = OnMatchStr,
			["initialState"] = InitialState,
			["types"] = TypesStr,
			_offRE = OffRE,
			_transitionToOffRE = TransitionToOffRE,
			_transitionToOnRE = TransitionToOnRE,
			_onRE = OnRE,
			_toggleRE = ToggleRE,
			_outputLookup = _outputLookup,
			_state = State,
			_types = Types,
			_defines = DefinesList,
			_consts = ConstsList,
			_fields = FieldsList,
			_properties = PropertiesList,
			_methods = MethodsList,
			_evals = EvalsList
		};
	}


	public override string ToString()
	{
		return $"{{{GetType().Name}: {Name
		}, <{"/".RCoalesce(OffMatchStr.NullIfEmpty(), "/") ?? "null"
		}, <<{"/".RCoalesce(TransitionToOffMatchStr.NullIfEmpty(), "/") ?? "null"
		}, >>{"/".RCoalesce(TransitionToOnMatchStr.NullIfEmpty(), "/") ?? "null"
		}, >{"/".RCoalesce(OnMatchStr.NullIfEmpty(), "/") ?? "null"
		}, ~{"/".RCoalesce(ToggleMatchStr.NullIfEmpty(), "/") ?? "null"
		}}}";
	}
}
