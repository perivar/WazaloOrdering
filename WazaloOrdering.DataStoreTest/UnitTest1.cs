using System;
using Xunit;
using WazaloOrdering.DataStore;
using Serilog;

namespace WazaloOrdering.DataStoreTest
{
    public class UnitTest1
    {
        [Fact]
        public void TestNormalizedEquivalentPhrase()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File("consoleapp.log")
                .CreateLogger();

            string norsk = "ÆØÅæøå";
            string norskNorm = Utils.GetNormalizedEquivalentPhrase(norsk);
            Log.Information("{0}={1}", norsk, norskNorm);
            Assert.Equal("AEOAaeoa", norskNorm, StringComparer.CurrentCultureIgnoreCase);

            string epsilon = "Ɛpsilon";
            string epsilonNorm = Utils.GetNormalizedEquivalentPhrase(epsilon);
            Log.Information("{0}={1}", epsilon, epsilonNorm);
            Assert.Equal("Epsilon", epsilonNorm, StringComparer.CurrentCultureIgnoreCase);

            string kobenhavn = "København";
            string kobenhavnNorm = Utils.GetNormalizedEquivalentPhrase(kobenhavn);
            Log.Information("{0}={1}", kobenhavn, kobenhavnNorm);
            Assert.Equal("Kobenhavn", kobenhavnNorm, StringComparer.CurrentCultureIgnoreCase);

            string angstrom = "Ångström";
            string angstromNorm = Utils.GetNormalizedEquivalentPhrase(angstrom);
            Log.Information("{0}={1}", angstrom, angstromNorm);
            Assert.Equal("Angstrom", angstromNorm, StringComparer.CurrentCultureIgnoreCase);

            string elnino = "El Niño";
            string elninoNorm = Utils.GetNormalizedEquivalentPhrase(elnino);
            Log.Information("{0}={1}", elnino, elninoNorm);
            Assert.Equal("El Nino", elninoNorm, StringComparer.CurrentCultureIgnoreCase);

            string tiengviet = "Tiếng Việt";
            string tiengvietNorm = Utils.GetNormalizedEquivalentPhrase(tiengviet);
            Log.Information("{0}={1}", tiengviet, tiengvietNorm);
            Assert.Equal("Tieng Viet", tiengvietNorm, StringComparer.CurrentCultureIgnoreCase);

            string cestina = "Čeština";
            string cestinaNorm = Utils.GetNormalizedEquivalentPhrase(cestina);
            Log.Information("{0}={1}", cestina, cestinaNorm);
            Assert.Equal("Cestina", cestinaNorm, StringComparer.CurrentCultureIgnoreCase);

            string encyklopaedi = "encyklopædi";
            string encyklopaediNorm = Utils.GetNormalizedEquivalentPhrase(encyklopaedi);
            Log.Information("{0}={1}", encyklopaedi, encyklopaediNorm);
            Assert.Equal("encyklopaedi", encyklopaediNorm, StringComparer.CurrentCultureIgnoreCase);

            string expeditia = "Expediția";
            string expeditiaNorm = Utils.GetNormalizedEquivalentPhrase(expeditia);
            Log.Information("{0}={1}", expeditia, expeditiaNorm);
            Assert.Equal("Expeditia", expeditiaNorm, StringComparer.CurrentCultureIgnoreCase);

            string odrum = "øðrum";
            string odrumNorm = Utils.GetNormalizedEquivalentPhrase(odrum);
            Log.Information("{0}={1}", odrum, odrumNorm);
            Assert.Equal("odrum", odrumNorm, StringComparer.CurrentCultureIgnoreCase);

            string oeuf = "œuf";
            string oeufNorm = Utils.GetNormalizedEquivalentPhrase(oeuf);
            Log.Information("{0}={1}", oeuf, oeufNorm);
            Assert.Equal("oeuf", oeufNorm, StringComparer.CurrentCultureIgnoreCase);

            string strasse = "Straße";
            string strasseNorm = Utils.GetNormalizedEquivalentPhrase(strasse);
            Log.Information("{0}={1}", strasse, strasseNorm);
            Assert.Equal("Strasse", strasseNorm, StringComparer.CurrentCultureIgnoreCase);
        }
    }
}
