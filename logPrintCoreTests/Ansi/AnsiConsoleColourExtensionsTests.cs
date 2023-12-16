using System.Reflection;

using logPrintCore.Ansi;

using PowerAssert;

using Xunit.Abstractions;

namespace logPrintCoreTests.Ansi;

// ReSharper disable once MemberCanBeFileLocal
public class AnsiConsoleColourExtensionsTests
{
	private readonly ITestOutputHelper _testOutputHelper;
	private static readonly MethodInfo _codeToAnsi = typeof(AnsiConsoleColourExtensions).GetMethod("CodeToAnsi", BindingFlags.NonPublic | BindingFlags.Static)!;


	public AnsiConsoleColourExtensionsTests(ITestOutputHelper testOutputHelper)
	{
		_testOutputHelper = testOutputHelper;
	}


	private static byte ToCode(string code)
	{
		return (byte)_codeToAnsi.Invoke(null, new object?[] { code, () => "" })!;
	}


	[Fact]
	public void TestParsing()
	{
		PAssert.IsTrue(() => AnsiConsoleColourExtensions.TextPartPool.IsEmpty);
		PAssert.IsTrue(() => AnsiConsoleColourExtensions.ResetPartPool.IsEmpty);
		PAssert.IsTrue(() => AnsiConsoleColourExtensions.ForegroundColourPartPool.IsEmpty);
		PAssert.IsTrue(() => AnsiConsoleColourExtensions.BackgroundColourPartPool.IsEmpty);
		PAssert.IsTrue(() => AnsiConsoleColourExtensions.PushPartPool.IsEmpty);
		PAssert.IsTrue(() => AnsiConsoleColourExtensions.PopPartPool.IsEmpty);

		var parseMethod = typeof(AnsiConsoleColourExtensions).GetMethod("Parse", BindingFlags.NonPublic | BindingFlags.Static);

		var tests = new List<(string? input, bool? resetAtEnd, Part[] results)> {
			(null, null,	Array.Empty<Part>()),
			(null, false,	Array.Empty<Part>()),
			(null, true,	Array.Empty<Part>()),
			("", null,	Array.Empty<Part>()),
			("", false,	Array.Empty<Part>()),
			("", true,	Array.Empty<Part>()),
			("a", null,		new Part[] { GetTextPart("a") }),
			("a", false,	new Part[] { GetTextPart("a") }),
			("a", true,		new Part[] { GetTextPart("a"), GetResetPart() }),
			("~r~a", null,	new Part[] { GetFGPart("r"), GetTextPart("a"), GetResetPart() }),
			("~r~a", false,	new Part[] { GetFGPart("r"), GetTextPart("a") }),
			("~r~a", true,	new Part[] { GetFGPart("r"), GetTextPart("a"), GetResetPart() }),
			("#r#a", null,	new Part[] { GetBGPart("r"), GetTextPart("a"), GetResetPart() }),
			("#r#a", false,	new Part[] { GetBGPart("r"), GetTextPart("a") }),
			("#r#a", true,	new Part[] { GetBGPart("r"), GetTextPart("a"), GetResetPart() }),
			("~W~#r#a", null,	new Part[] { GetFGPart("W"), GetBGPart("r"), GetTextPart("a"), GetResetPart() }),
			("~W~#r#a", false,	new Part[] { GetFGPart("W"), GetBGPart("r"), GetTextPart("a") }),
			("~W~#r#a", true,	new Part[] { GetFGPart("W"), GetBGPart("r"), GetTextPart("a"), GetResetPart() }),
			("#r#~W~a", null,	new Part[] { GetBGPart("r"), GetFGPart("W"), GetTextPart("a"), GetResetPart() }),
			("#r#~W~a", false,	new Part[] { GetBGPart("r"), GetFGPart("W"), GetTextPart("a") }),
			("#r#~W~a", true,	new Part[] { GetBGPart("r"), GetFGPart("W"), GetTextPart("a"), GetResetPart() }),
			("~<~#<##r#~W~a#>#~>~", null,	new Part[] { GetPushPart(true), GetPushPart(false), GetBGPart("r"), GetFGPart("W"), GetTextPart("a"), GetPopPart(false), GetPopPart(true), GetResetPart() }),
			("~<~#<##r#~W~a#>#~>~", false,	new Part[] { GetPushPart(true), GetPushPart(false), GetBGPart("r"), GetFGPart("W"), GetTextPart("a"), GetPopPart(false), GetPopPart(true) }),
			("~<~#<##r#~W~a#>#~>~", true,	new Part[] { GetPushPart(true), GetPushPart(false), GetBGPart("r"), GetFGPart("W"), GetTextPart("a"), GetPopPart(false), GetPopPart(true), GetResetPart() }),
		};

		foreach (var test in tests) {
			_testOutputHelper.WriteLine(
				(test.input is null)
					? $"from: null (reset: {test.resetAtEnd})"
					: $"from: `{test.input}` (reset: {test.resetAtEnd})"
			);

			_testOutputHelper.WriteLine($" => [{string.Join(", ", test.results.Select(x => x.ToString()))}]");

			var result = ((IEnumerable<Part>)parseMethod!.Invoke(null, new object?[] { test.input, test.resetAtEnd })!)
				.ToList();

			// Issues with using the wrong .Equals: PAssert.IsTrue(() => test.to.SequenceEqual(result));
			for (var i = 0; i < result.Count; i++) {
				// ReSharper disable once AccessToModifiedClosure
				PAssert.IsTrue(() => i < test.results.Length);
				var expected = test.results[i];
				var actual = result[i];
				PAssert.IsTrue(() => actual.GetType() == expected.GetType());
				PAssert.IsTrue(() => actual == expected);
			}

			foreach (var part in test.results) {
				AnsiConsoleColourExtensions.Return(part);
			}

			foreach (var part in result) {
				AnsiConsoleColourExtensions.Return(part);
			}
		}

		PAssert.IsTrue(() => AnsiConsoleColourExtensions.TextPartPool.IsEmpty);
		PAssert.IsTrue(() => AnsiConsoleColourExtensions.ResetPartPool.IsEmpty);
		PAssert.IsTrue(() => AnsiConsoleColourExtensions.ForegroundColourPartPool.IsEmpty);
		PAssert.IsTrue(() => AnsiConsoleColourExtensions.BackgroundColourPartPool.IsEmpty);
		PAssert.IsTrue(() => AnsiConsoleColourExtensions.PushPartPool.IsEmpty);
		PAssert.IsTrue(() => AnsiConsoleColourExtensions.PopPartPool.IsEmpty);
	}


	[Fact]
	public void TestNormalising()
	{
		PAssert.IsTrue(() => AnsiConsoleColourExtensions.TextPartPool.IsEmpty);
		PAssert.IsTrue(() => AnsiConsoleColourExtensions.ResetPartPool.IsEmpty);
		PAssert.IsTrue(() => AnsiConsoleColourExtensions.ForegroundColourPartPool.IsEmpty);
		PAssert.IsTrue(() => AnsiConsoleColourExtensions.BackgroundColourPartPool.IsEmpty);
		PAssert.IsTrue(() => AnsiConsoleColourExtensions.PushPartPool.IsEmpty);
		PAssert.IsTrue(() => AnsiConsoleColourExtensions.PopPartPool.IsEmpty);

		var normaliseMethod = typeof(AnsiConsoleColourExtensions).GetMethod("Normalise", BindingFlags.NonPublic | BindingFlags.Static)!;

		var tests = new List<(bool debug, Part[] from, Part[] to)> {
			(
				false,
				Array.Empty<Part>(),
				Array.Empty<Part>()
			),
			(
				false,
				new Part[] { GetTextPart("a") },
				new Part[] { GetTextPart("a") }
			),
			(
				false,
				new Part[] { GetTextPart("a"), GetResetPart() },
				new Part[] { GetTextPart("a") }
			),
			(
				false,
				new Part[] { GetFGPart("r"), GetTextPart("a") },
				new Part[] { GetFGPart("r"), GetTextPart("a") }
			),
			(
				false,
				new Part[] { GetFGPart("r"), GetTextPart("a"), GetResetPart() },
				new Part[] { GetFGPart("r"), GetTextPart("a"), GetResetPart() }
			),
			(
				false,
				new Part[] { GetBGPart("r"), GetTextPart("a") },
				new Part[] { GetBGPart("r"), GetTextPart("a") }
			),
			(
				false,
				new Part[] { GetBGPart("r"), GetTextPart("a"), GetResetPart() },
				new Part[] { GetBGPart("r"), GetTextPart("a"), GetResetPart() }
			),
			(
				false,
				new Part[] { GetFGPart("W"), GetBGPart("r"), GetTextPart("a") },
				new Part[] { Merged<BackgroundColourPart>("W", "r"), GetTextPart("a") }
			),
			(
				false,
				new Part[] { GetFGPart("W"), GetBGPart("r"), GetTextPart("a"), GetResetPart() },
				new Part[] { Merged<BackgroundColourPart>("W", "r"), GetTextPart("a"), GetResetPart() }
			),
			(
				false,
				new Part[] { GetPushPart(true), GetPushPart(false), GetBGPart("r"), GetFGPart("W"), GetTextPart("a"), GetPopPart(false), GetPopPart(true) },
				new Part[] { MergedPush(Part.DefaultForeground, Part.DefaultBackground), Merged<ForegroundColourPart>("W", "r"), GetTextPart("a"), MergedPop(Part.DefaultForeground, Part.DefaultBackground) }
			),
			(
				false,
				new Part[] { GetPushPart(true), GetPushPart(false), GetBGPart("r"), GetFGPart("W"), GetTextPart("a"), GetPopPart(false), GetPopPart(true), GetResetPart() },
				new Part[] { MergedPush(Part.DefaultForeground, Part.DefaultBackground), Merged<ForegroundColourPart>("W", "r"), GetTextPart("a"), MergedPop(Part.DefaultForeground, Part.DefaultBackground) }
			),
		};

		foreach (var test in tests) {
			_testOutputHelper.WriteLine("from:" + string.Join(", ", test.from.Select(x => x.ToString())));
			_testOutputHelper.WriteLine("  to:" + string.Join(", ", test.to.Select(x => x.ToString())));

			if (test.debug && System.Diagnostics.Debugger.IsAttached) {
				System.Diagnostics.Debugger.Break();
			}

			var result = ((IEnumerable<Part>)normaliseMethod.Invoke(null, new object?[] { test.from })!)
				.ToList();

			_testOutputHelper.WriteLine(" got:" + string.Join(", ", result.Select(x => x.ToString())));
			_testOutputHelper.WriteLine("");

			// Issues with using the wrong .Equals: PAssert.IsTrue(() => test.to.SequenceEqual(result));
			for (var i = 0; i < result.Count; i++) {
				// ReSharper disable once AccessToModifiedClosure
				PAssert.IsTrue(() => i < test.to.Length);
				var expected = test.to[i];
				var actual = result[i];
				PAssert.IsTrue(() => actual.GetType() == expected.GetType());
				PAssert.IsTrue(() => actual == expected);
			}

			using var poly = PAssert.Poly();

			poly.IsTrue(() => test.to.Length == result.Count);
			poly.IsTrue(() => !result.Skip(test.to.Length).Any());

			// No need to return the from as that is handled by returning result below.

			foreach (var part in test.to) {
				AnsiConsoleColourExtensions.Return(part);
			}

			foreach (var part in result) {
				AnsiConsoleColourExtensions.Return(part);
			}
		}

		PAssert.IsTrue(() => AnsiConsoleColourExtensions.TextPartPool.IsEmpty);
		PAssert.IsTrue(() => AnsiConsoleColourExtensions.ResetPartPool.IsEmpty);
		PAssert.IsTrue(() => AnsiConsoleColourExtensions.ForegroundColourPartPool.IsEmpty);
		PAssert.IsTrue(() => AnsiConsoleColourExtensions.BackgroundColourPartPool.IsEmpty);
		PAssert.IsTrue(() => AnsiConsoleColourExtensions.PushPartPool.IsEmpty);
		PAssert.IsTrue(() => AnsiConsoleColourExtensions.PopPartPool.IsEmpty);
	}


	private static TextPart GetTextPart(string text)
	{
		return AnsiConsoleColourExtensions.TextPartPool.Rent().Init(text);
	}

	private static ResetPart GetResetPart(bool? fg = null)
	{
		return AnsiConsoleColourExtensions.ResetPartPool.Rent().Init(fg);
	}

	private static ForegroundColourPart GetFGPart(string c)
	{
		return AnsiConsoleColourExtensions.ForegroundColourPartPool.Rent().Init(ToCode(c));
	}

	private static BackgroundColourPart GetBGPart(string c)
	{
		return AnsiConsoleColourExtensions.BackgroundColourPartPool.Rent().Init(ToCode(c));
	}

	private static PushPart GetPushPart(bool? fg)
	{
		return AnsiConsoleColourExtensions.PushPartPool.Rent().Init(fg);
	}

	private static PopPart GetPopPart(bool? fg)
	{
		return AnsiConsoleColourExtensions.PopPartPool.Rent().Init(fg);
	}


	private static T Merged<T>(string fg, string bg)
		where T : ColourPart
	{
		var type = typeof(T);

		ColourPart result = type == typeof(ForegroundColourPart)
			? AnsiConsoleColourExtensions.ForegroundColourPartPool.Rent()
			: AnsiConsoleColourExtensions.BackgroundColourPartPool.Rent();

		type.GetField("_isForeground", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(result, null);
		type.GetField("_currentForeground", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(result, ToCode(fg));
		type.GetField("_currentBackground", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(result, ToCode(bg));

		return (T)result;
	}

	private static PushPart MergedPush(byte? pushedFg, byte? pushedBg)
	{
		var type = typeof(PushPart);

		var push = AnsiConsoleColourExtensions.PushPartPool.Rent();
		push._pushedForeground = pushedFg ?? push._pushedForeground;
		push._pushedBackground = pushedBg ?? push._pushedBackground;

		type.GetField("_isForeground", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(push, null);
		type.GetField("_currentForeground", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(push, (byte)0xE0);
		type.GetField("_currentBackground", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(push, (byte)0xE0);

		return push;
	}

	private static PopPart MergedPop(byte fg, byte bg)
	{
		var type = typeof(PopPart);

		var pop = AnsiConsoleColourExtensions.PopPartPool.Rent();

		type.GetField("_isForeground", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(pop, null);
		type.GetField("_currentForeground", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(pop, fg);
		type.GetField("_currentBackground", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(pop, bg);

		return pop;
	}
}
