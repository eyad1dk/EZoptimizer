using ForgePC;
using System.Text.Json;
var root = Path.Combine(Path.GetTempPath(), "EZoptimizer-tests-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
var passed = 0;
void Test(string name, Action test) { test(); passed++; Console.WriteLine("PASS " + name); }
void Assert(bool condition) { if (!condition) throw new Exception("Assertion failed."); }
void Throws(Action action) { try { action(); } catch (InvalidOperationException) { return; } throw new Exception("Expected failure."); }
(FakeSettings, TuningEngine) Fixture()
{
 var settings = new FakeSettings();
 return (settings, new TuningEngine(settings, Path.Combine(root, Guid.NewGuid().ToString("N"))));
}
List<Change> Changes() => [new("animations", "Animations", "On", "Off"), new("menus", "Menus", "On", "Off")];
try
{
 Test("apply is verified and original settings survive a restart", () => {
  var (s, engine) = Fixture(); var j = engine.Apply("Gaming", Changes());
  Assert(s.Values.Values.All(v => v == "Off"));
  Assert(engine.History().Single().Changes.Count == 2);
  Assert(engine.Restore(engine.History().Single()).Count == 0);
  Assert(s.Values.Values.All(v => v == "On")); Assert(engine.History().Single().Status == "restored");
 });
 Test("stale preview cannot overwrite changed settings", () => {
  var (s, engine) = Fixture(); s.Values["menus"] = "Off"; Throws(() => engine.Apply("Coding", Changes()));
  Assert(s.Writes == 0); Assert(engine.History().Count == 0);
 });
 Test("a failed write has a persisted recovery record", () => {
  var (s, engine) = Fixture(); s.FailKey = "menus"; Throws(() => engine.Apply("Gaming", Changes()));
  Assert(engine.History().Single().Status == "interrupted");
  s.FailKey = null; Assert(engine.Restore(engine.History().Single()).Count == 0);
  Assert(s.Values.Values.All(v => v == "On"));
 });
 Test("a crash after writing is recoverable from recorded intent", () => {
  var (s, engine) = Fixture(); s.WriteThenFail = true; Throws(() => engine.Apply("Coding", Changes()));
  s.WriteThenFail = false; Assert(engine.Restore(engine.History().Single()).Count == 0);
  Assert(s.Values["animations"] == "On");
 });
 Test("undo preserves settings changed outside the app", () => {
  var (s, engine) = Fixture(); engine.Apply("Gaming", Changes()); s.Values["menus"] = "External";
  Assert(engine.Restore(engine.History().Single()).Count == 1);
  Assert(s.Values["menus"] == "External"); Assert(s.Values["animations"] == "On");
  s.Values["menus"] = "Off"; Assert(engine.Restore(engine.History().Single()).Count == 0);
 });
 Test("only one session can be active, and undo is idempotent", () => {
  var (s, engine) = Fixture(); var j = engine.Apply("Gaming", Changes());
  Throws(() => engine.Apply("Coding", [new("animations", "Animations", "Off", "On")]));
  engine.Restore(j); var count = s.Writes; engine.Restore(j); Assert(s.Writes == count);
  engine.Apply("Coding", Changes()); Assert(engine.History().Count == 2);
 });
 Test("silent Windows write rejection leaves recoverable history", () => {
  var (s, engine) = Fixture(); s.IgnoreWrites = true; Throws(() => engine.Apply("Gaming", Changes()));
  Assert(engine.History().Single().Status == "interrupted");
  Assert(engine.Restore(engine.History().Single()).Count == 0);
 });
 Test("empty plans cannot create sessions", () => {
  var (s, engine) = Fixture(); Throws(() => engine.Apply("Gaming", [])); Assert(engine.History().Count == 0);
 });
 Console.WriteLine($"{passed} safety checks passed. No Windows settings changed.");
}
finally { Directory.Delete(root, true); }
sealed class FakeSettings : ISettings
{
 public Dictionary<string, string> Values = new() { ["animations"] = "On", ["menus"] = "On" };
 public string? FailKey;
 public bool WriteThenFail, IgnoreWrites;
 public int Writes;
 public string Read(string key) => Values[key];
 public void Write(string key, string value)
 {
  Writes++;
  if (key == FailKey) throw new IOException("Simulated write failure.");
  if (!IgnoreWrites) Values[key] = value;
  if (WriteThenFail) throw new IOException("Simulated interruption after write.");
 }
}
