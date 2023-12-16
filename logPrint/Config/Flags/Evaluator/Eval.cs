using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

using Microsoft.CSharp;

using logPrint.Ansi;
using logPrint.Utils;

namespace logPrint.Config.Flags.Evaluator;

internal sealed class Eval : ConfigurationElement, IEvaluator
{
	[ConfigurationProperty("when", IsRequired = true, IsKey = true)]
	internal string When => this["when"] as string;

	[ConfigurationProperty("output", IsRequired = true)]
	internal string Output => this["output"] as string;


	IEvaluator _evaluator;


	internal string Process(string line, Flag parent)
	{
		return PopulateValues(line, parent) && _evaluator.Eval()
			? Output
			: null;
	}

	// ReSharper disable once UnusedParameter.Global - needed to make a distinct overload:
	internal void Reset(Flag parent)
	{
		_evaluator?.Reset();
	}


	bool PopulateValues(string line, Flag parent)
	{
		if (!parent.TransitionToOnRE?.IsMatch(line) ?? false) {
			return false;
		}


		if (_evaluator == null) {
			Console.Out.WriteColours($"#y# ~Y~Compiling: ~W~#!#{this}\r");
			GenerateExpression(When, parent, unchecked(parent.TypesStr.GetHashCode() * 397 ^ When.GetHashCode()));
			Console.Out.ClearLine();
		}

		_evaluator.SetValues(parent.OnRE.Match(line));
		if (parent.MethodsList.Any()) {
			_evaluator.CallMethods();
		}

		return true;
	}

	void GenerateExpression(string code, Flag parent, int hashCode)
	{
		var existingType = AppDomain.CurrentDomain
			.GetAssemblies()
			.Select(assembly => assembly.GetType("logPrint.Flags.Evaluator.Evaluator" + (uint)hashCode))
			.FirstOrDefault(type => type != null);

		if (existingType != null) {
			_evaluator = (IEvaluator)existingType
				.GetConstructor(Type.EmptyTypes)
				?.Invoke(new object[0]);

			return;
		}


		CompilerResults compilerResults = CompileEvaluator(code, parent, hashCode, out CodeCompileUnit compileUnit, out CSharpCodeProvider provider);

		if (compilerResults.Errors.Count > 0) {
			this.Dump("Eval", escapeColours: false);
			compilerResults.Errors.Dump(multiLine: true);
			var errorLookup = compilerResults.Errors
				.Cast<CompilerError>()
				.Select(
					err => new {
						err.Line,
						Message = $"#K#   ~Y~!{new string('-', err.Column)}~Y~^ ~w~: #w#{(
							err.IsWarning
								? "~M~warning"
								: "~R~error"
						)} {err.ErrorNumber} ~Y~: ~W~{err.ErrorText}#!#"
					}
				);

			string generatedCode = GetGeneratedCode(provider, compileUnit);
			(
				Environment.NewLine
				+ string.Join(
					Environment.NewLine,
					generatedCode
						.Replace("\r", "")
						.Replace("\t", " ")
						.Split('\n')
						.Select(
							(line, num) => $"#c#~C~{num + 1:D3}~Y~:#!# ~W~{line.EscapeColourCodeChars()}{
								Environment.NewLine.RCoalesce(errorLookup.FirstOrDefault(err => err.Line == num + 1)?.Message)
							}"
						)
				)
			).Dump("Generated Code", escapeColours: false);

			return;
		}


		_evaluator = (IEvaluator)compilerResults
			.CompiledAssembly
			.GetTypes()
			.First()
			.GetConstructor(Type.EmptyTypes)
			?.Invoke(new object[0]);
	}

	static CompilerResults CompileEvaluator(string code, Flag parent, int hashCode, out CodeCompileUnit compileUnit, out CSharpCodeProvider provider)
	{
		var evalMethod = new CodeMemberMethod {
			Name = "Eval",
			Attributes = MemberAttributes.Public,
			ReturnType = new CodeTypeReference(typeof(bool)),
			Statements = {
				new CodeSnippetExpression($"return ({((code == "Else") ? code : $"!Else && ({code})")})")
			}
		};

		static string CleanTemplate(string template)
		{
			return template.Replace("\t", "    ").TrimStart(Environment.NewLine.ToCharArray()).TrimEnd();
		}

		var staticClass = new CodeTypeDeclaration("Evaluator" + (uint)hashCode) {
			Members = {
				evalMethod,
				new CodeSnippetTypeMember(CleanTemplate(ELSE_PROPERTY)),
				new CodeSnippetTypeMember(CleanTemplate(SET_VALUES_METHOD)),
				new CodeSnippetTypeMember(CleanTemplate(TO_STRING_METHOD.Replace("%", code.Replace("\"", @"\"""))))
			},
			BaseTypes = {
				typeof(IEvaluator)
			}
		};

		staticClass.Members.AddRange(
			parent.DefinesList
				.Select(define => new CodeSnippetTypeMember(string.Format(CleanTemplate(CONST), define.Type, define.Name, define.Value(parent.SelectedDefines))))
				.Concat(parent.ConstsList.Select(@const => new CodeSnippetTypeMember(string.Format(CleanTemplate(CONST), @const.Type, @const.Name, @const.Value))))
				.Concat(parent.FieldsList.Select(field => new CodeSnippetTypeMember(string.Format(CleanTemplate(FIELD), field.Type, field.Name, field.Value))))
				.Concat(
					parent.PropertiesList
						.Select(
							property => new CodeSnippetTypeMember(
								string.Format(
									CleanTemplate(PROPERTY),
									property.Type,
									property.Name,
									property.Code.Contains("return")
										? property.Code
										: "return " + property.Code
								)
							)
						)
				)
				.Concat(
					parent.MethodsList
						.Select(
							method => new CodeSnippetTypeMember(
								string.Format(
									(method.Name == "Reset")
										? CleanTemplate(METHOD)
											.Replace("private", "public")
										: CleanTemplate(METHOD),
									method.Name,
									method.Code
								)
							)
						)
				)
				.Cast<CodeTypeMember>()
				.ToArray()
		);

		if (parent.MethodsList.All(method => method.Name != "Reset")) {
			staticClass.Members.Add(
				new CodeSnippetTypeMember(
					Regex.Replace(
						CleanTemplate(CALL_METHODS)
							.Replace("CallMethods", "Reset"),
						@"^\s+%[\n\r]+",
						"",
						RegexOptions.Multiline
					)
				)
			);
		}

		staticClass.Members.Add(
			new CodeSnippetTypeMember(
				Regex.Replace(
					CleanTemplate(CALL_METHODS),
					@"^(\s+)%",
					string.Join(
						Environment.NewLine + "$1",
						parent.MethodsList
							.Where(method => method.Name != "Reset")
							.Select(method => method.Name + "();")
					),
					RegexOptions.Multiline
				)
			)
		);

		parent.Types
			.ToList()
			.ForEach(
				pair => {
					var type = new CodeTypeReference(pair.Value);
					staticClass.Members.Add(
						new CodeMemberField(type, pair.Key) {
							Attributes = MemberAttributes.Public,
							InitExpression = new CodeDefaultValueExpression(type)
						}
					);
				}
			);

		var ns = new CodeNamespace("logPrint.Flags.Evaluator") {
			Types = {
				staticClass
			},
			Imports = {
				new CodeNamespaceImport("System"),
				new CodeNamespaceImport("System.Linq"),
				new CodeNamespaceImport("System.Reflection"),
				new CodeNamespaceImport("System.Text.RegularExpressions"),
				new CodeNamespaceImport("logPrint.Utils")
			}
		};

		compileUnit = new CodeCompileUnit { Namespaces = { ns } };

		provider = new CSharpCodeProvider();
		var compilerParameters = new CompilerParameters(
			new[] {
				"System.dll",
				"System.Core.dll",
				Assembly.GetExecutingAssembly().Location
			}
		) {
#if DEBUG
			OutputAssembly = "Evaluator" + (uint)hashCode,
			IncludeDebugInformation = true,
#else
			GenerateInMemory = true
#endif
		};

		var compilerResults = provider.CompileAssemblyFromDom(compilerParameters, compileUnit);
#if DEBUG
		File.WriteAllText(compilerResults.TempFiles.BasePath + ".0.cs", GetGeneratedCode(provider, compileUnit));
#endif
		return compilerResults;
	}

	// ReSharper disable once SuggestBaseTypeForParameter - I explicitly only deal with C# here.
	static string GetGeneratedCode(CSharpCodeProvider provider, CodeCompileUnit compileUnit)
	{
		var sb = new StringBuilder();
		using var stringWriter = new StringWriter(sb);
		var textWriter = new IndentedTextWriter(stringWriter);
		provider.GenerateCodeFromCompileUnit(compileUnit, textWriter, new CodeGeneratorOptions());
		textWriter.Close();

		return sb.ToString();
	}

	#region Code Templates

	public bool Else { get; private set; }

	bool IEvaluator.Eval()
	{
		return false;
	}

	public void SetValues(Match match)
	{
		Else = !match.Success;
		if (Else) {
			return;
		}


		var type = GetType();

		foreach (Group matchGroup in match.Groups) {
			if (char.IsDigit(matchGroup.Name[0])) {
				continue;
			}


			var field = type.GetField(matchGroup.Name, BindingFlags.Public | BindingFlags.Instance);
			if (field == null) {
				throw new MemberAccessException($"Field {matchGroup.Name} not found on {type.FullName}!");
			}


			if (field.FieldType == typeof(string)) {
				field.SetValue(this, match.Value);
				continue;
			}


			if (field.FieldType.ToString().StartsWith("System.Nullable`1[", StringComparison.Ordinal)) {
				if (matchGroup.Success) {
					field.SetValue(
						this,
						field.FieldType.GenericTypeArguments[0]
							.GetMethods(BindingFlags.Public | BindingFlags.Static)
							.First(
								method => {
									if (method.Name != "Parse") {
										return false;
									}


									var parameters = method.GetParameters();
									return (parameters.Length == 1 && parameters[0].ParameterType == typeof(string));
								}
							)
							.Invoke(null, new object[] { matchGroup.Value })
					);
				} else {
					field.SetValue(this, null);
				}
			} else if (matchGroup.Success) {
				field.SetValue(
					this,
					field.FieldType
						.GetMethods(BindingFlags.Public | BindingFlags.Static)
						.First(
							method => {
								if (method.Name != "Parse") {
									return false;
								}


								var parameters = method.GetParameters();
								return (parameters.Length == 1 && parameters[0].ParameterType == typeof(string));
							}
						)
						.Invoke(null, new object[] { matchGroup.Value })
				);
			}
		}
	}

	public void CallMethods()
	{
		// Keep R# happy:
		//Console.WriteLine(((IEvaluator)this).Else);
	}

	public void Reset() { }


	const string ELSE_PROPERTY = @"
		public bool Else { get; private set; }
		";

	const string SET_VALUES_METHOD = @"
		public void SetValues(Match match) {
			Else = !match.Success;
			if (Else) {
				return;
			}


			var type = GetType();

			foreach (Group matchGroup in match.Groups) {
				if (!char.IsDigit(matchGroup.Name[0])) {
					var field = type.GetField(matchGroup.Name, BindingFlags.Public | BindingFlags.Instance);
					if (field == null) {
						throw new MemberAccessException(string.Concat(""Field "", matchGroup.Name, "" not found on "", type.FullName, ""!""));
					}


					if (field.FieldType == typeof(string)) {
						field.SetValue(this, match.Value);
						continue;
					}


					if (field.FieldType.ToString().StartsWith(""System.Nullable`1["")) {
						if (matchGroup.Success) {
							field.SetValue(
								this,
								field.FieldType.GenericTypeArguments[0]
									.GetMethods(BindingFlags.Public | BindingFlags.Static)
									.First(
										method => {
											if (method.Name != ""Parse"") {
												return false;
											}


											var parameters = method.GetParameters();
											return (parameters.Length == 1 && parameters[0].ParameterType == typeof(string));
										}
									)
									.Invoke(null, new object[] { matchGroup.Value })
							);
						} else {
							field.SetValue(this, null);
						}
					} else if (matchGroup.Success) {
						field.SetValue(
							this,
							field.FieldType
								.GetMethods(BindingFlags.Public | BindingFlags.Static)
								.First(
									method => {
										if (method.Name != ""Parse"") {
											return false;
										}


										var parameters = method.GetParameters();
										return (parameters.Length == 1 && parameters[0].ParameterType == typeof(string));
									}
								)
								.Invoke(null, new object[] { matchGroup.Value })
						);
					}
				}
			}
		}
		";

	const string CALL_METHODS = @"
		public void CallMethods() {
			%
		}
		";

	const string CONST = @"
		private const {0} {1} = {2};
		";

	const string FIELD = @"
		private {0} {1} = {2};
		";

	const string PROPERTY = @"
		private {0} {1} {{ get {{ {2}; }} }}
		";

	const string METHOD = @"
		private void {0}()
		{{
			{1}
		}}
		";

	const string TO_STRING_METHOD = @"
		public override string ToString()
		{
			return string.Format(
				""{{{0}: Eval(#<##y#%#>#)=~<~{1}; {2}~>~}}"",
				GetType().Name,
				Eval() ? ""~<~~C~True~>~"" : ""~<~~M~False~>~"",
				string.Join(
					""~W~, "",
					GetType()
						.GetFields(BindingFlags.Public | BindingFlags.Instance)
						.Select(
							x => string.Format(
								""~>~{0}=~<~~G~{1}"",
								x.Name,
								x.GetValue(this)
							)
						)
				)
			);
		}
		";

	#endregion

	public override string ToString()
	{
		return $"{{{GetType().Name}: {nameof(When)}={When}, {nameof(Output)}=#<#~<~{Output}~>~#>#, {nameof(_evaluator)}:{_evaluator}}}";
	}
}
