﻿

module Main
// Learn more about F# at http://fsharp.net
// See the 'F# Tutorial' project for more help.


open System.Diagnostics

open System

open SolidTests.Targets
open SolidTests.Tests
open System.Collections.Generic
open System.Threading
open SolidFS

open MapTestTargets
open SolidTests.Generators
let inline ( ** ) a b = pown a b



type MutableList<'a> = System.Collections.Generic.List<'a>
let warmup = 3
let sw = Stopwatch()
let mutable time = TimeSpan()
let mutable maybe_failed = false

type TestOutcome =
    | Failed
    | Succeeded of float

let invoke_test action o =
    maybe_failed <- false
    
    let thread = Thread(fun () ->
            try
            for __ = 0 to warmup do
                action o
            GC.Collect()
            sw.Restart()
            for __ = 0 to warmup do
                action o
            sw.Stop()
            time <- sw.Elapsed
            with //empirically this try-with block does not affect performance.
            | :? TestNotAvailableException<int> as x -> maybe_failed <- true
            )
    thread.Priority <- ThreadPriority.Highest
    
    thread.Start()
    thread.Join()

    if maybe_failed then Failed else Succeeded(time.TotalMilliseconds/4.)



type TestResult = {Name : string; Result : TestOutcome}
type TestExecution = {Name : string; Results : MutableList<TestResult>}
let run_test_on (action : Test) (objs : list<TestTarget<int>>) =
    let results = MutableList()
    for target in objs do
        //printfn "Beginning test %s" target.Name
        let ret = target |> invoke_test (action.Test >> ignore)
        results.Add({Name = target.Name; Result=ret})



    {Name = action.Name; Results = results}

let run_test_on_map (action : MapTest<_>) (objs : list<MapTestTarget<_>>)= 
    let results = MutableList()
    for target in objs do
        let ret = target |> invoke_test (action.Test >> ignore)
        results.Add({Name = target.Name; Result = ret})
    {Name = action.Name; Results = results}
let run_test_sequence (the_sequence : Test list) get_targets =
    seq {
        for test in the_sequence do
            let current_result = run_test_on test (get_targets())
            yield current_result
        }


let run_test_sequence_map (the_sequence : MapTest<_> list) get_targets = 
    seq {
        for test in the_sequence do
            let current_result = run_test_on_map test (get_targets())
            yield current_result
    }
open SolidTests.Tests

open SolidTests.Targets

let print {Name = name; Results = results} = printfn "%s" name; results |> Seq.iter (fun t -> printfn "%A" t)


let perf_testing() =
    let lst = MutableList<int>()
    let lst2 = MutableList<int>()
    let sw = Stopwatch()
    let iterations = 10**5
    System.Console.BufferHeight <- Console.BufferHeight * 3
    let all_tests = [Test_insert_ascending iterations;
        Test_add_first iterations;
        Test_add_last iterations;
        Test_set_rnd iterations;
        Test_remove_rnd iterations;
        Test_set_each ;
        //Test_iter_take_first iterations;
        Test_get_rnd <|10**5;
        Test_get_each ;
        Test_rem_all;
        Test_concat_self iterations;
        Test_iter_take_first iterations]
    let results = run_test_sequence all_tests (delay1 all_test_targets iterations)
    results |> Seq.iter print

let perf_testing_maps() = 
    let iterations = (10 ** 5)
    let sGenerator = {Bounds = 30, 50; Chars=  [| 'a' .. 'z'|]; Count = iterations} :> IGenerator<_>
    let sGenerator2 = {Bounds = 30, 50; Chars = [| 'A' .. 'Z'|]; Count = iterations} :> IGenerator<_>
    let input = sGenerator.Generate() |> Seq.toArray
    let input2 =sGenerator2.Generate() |> Seq.toArray
    System.Console.BufferHeight <- Console.BufferHeight * 3
    let test_list = 
        [
            Test_add_many input2;
            //Test_rem_many input;
            Test_get_many input;
            Test_contains_many input
        ]
    let results = run_test_sequence_map test_list (fun () -> [FSMapTestTarget<_>.FromSeq input; SolidMapTestTarget<_>.FromSeq input])
    results |> Seq.iter print
let then_verify (x : TestTarget<_>) =
    if x.Verify() |> not then
        failwith "Error"
    else
        x

let unit_consistency_testing() =
    let mutable group = TestGroup([| solid_xlist 0; core_list 0|]) :> TestTarget<_>
    let mutable group2 = TestGroup([| core_list 0; solid_vector 0 |]) :> TestTarget<_>
    let iters = 10 ** 4
    group <- group |> test_addl_many iters |> then_verify
    group <- group |> test_addf_many iters |> then_verify
    group <- group |> test_add_mixed iters 0.5 |> then_verify
    group <- group |> test_insert_ascending iters |> then_verify
    group <- group |> test_set_rnd iters |> then_verify
    group |> test_get_each |> ignore
    group <- group |> test_set_each |> then_verify
    group <- group |> test_drop_all_mixed |> then_verify
    group <- group |> test_addl_many iters |> then_verify
    group <- group |> test_addf_many iters |> then_verify
    group |> test_dropl_num_then_verify 100 |> ignore
    group2 <- group2 |> test_addl_many iters |> then_verify
    group |> test_dropf_num_then_verify 100 |> ignore
    group2 <- group2 |> test_iterate_take_verify 100
    group2 <- group2 |> test_set_rnd iters |> then_verify
    group2 <- group2 |> test_get_rnd iters
    group2 <- group2 |> test_set_each |> then_verify
    group2 <- group2 |> test_dropl_num_then_verify 100
    group2 <- group2 |> test_addl_many 10000
    group2 <- group2 |> test_dropl_all |> then_verify
    
    test_iterate_slices 10 group
   
open SolidFS.Operators
open Solid


[<EntryPoint>]
let main argv =
   
    0