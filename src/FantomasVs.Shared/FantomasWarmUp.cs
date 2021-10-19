extern alias FantomasLatest;

using Fantomas;
using FantomasLatest::Fantomas;
using FSharp.Compiler.SourceCodeServices;
using Microsoft.FSharp.Control;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace FantomasVs
{
    public static class FantomasWarmUp
    {
        // https://gist.github.com/deviousasti/783c8a66dd711067b4f404d59d6b8e0b
        public static string SampleCode =>
            @"

let shuffle deck =
    let n = Array.length deck
    let rnd = new Random()
    let pick n = rnd.Next n
    let swap i j = 
        let a,b = deck.[i], deck.[j]
        Array.set deck j a
        Array.set deck i b

    for i = n - 1 downto 0 do
        swap i (pick i)
    deck

type Suit = Clubs | Diamonds | Hearts | Spades
type Value = 
    | Ace    = 1
    | ``2``  = 2
    | ``3``  = 3
    | ``4``  = 4
    | ``5``  = 5
    | ``6``  = 6
    | ``7``  = 7
    | ``8``  = 8
    | ``9``  = 9
    | ``10`` = 10
    | Jack   = 11
    | Queen  = 12
    | King   = 13

type Cards = Card of suit: Suit * value: Value
let values = [1..13] |> List.map enum<Value>
let suits = [Clubs; Diamonds; Hearts; Spades]
let deck = List.allPairs suits values |> List.map Card

shuffle (deck |> List.toArray) 


            ";

        public static async Task WarmUpAsync()
        {
            try
            {
                var fsasync = CodeFormatter.FormatDocumentAsync("tmp.fs", SourceOrigin.SourceOrigin.NewSourceString(SampleCode), FormatConfig.FormatConfig.Default, FSharpParsingOptions.Default, FSharpChecker.Create(null, null, null, null, null, null, null, null, null));
                var _ = await FSharpAsync.StartAsTask(fsasync, null, null);
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
            }
            
        }
    }
}
