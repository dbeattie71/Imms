[<AutoOpen>]
module Imms.FSharp.Implementation.Compatibility
open Imms
open Imms.FSharp
open System
type ComplRep = CompilationRepresentationAttribute
type ComplFlags = CompilationRepresentationFlags

let toFunc1 f = Func<_,_>(f)
let toFunc2 f = Func<_,_,_>(f)
let toFunc3 f = Func<_,_,_,_>(f)
let toAction (f : 'a -> unit) = Action<'a>(f)
let toPredicate (f : 'a -> bool) = Predicate<'a>(f)
let toConverter (f : 'a -> 'b) = Converter(f)
let toOption maybe =
    match maybe with
    | Some v -> Imms.Optional.Some v
    | None -> Imms.Optional.NoneOf()
let fromOption (c_option : Imms.Optional<_>) = 
    if c_option.IsSome then Some c_option.Value else None
let (|Kvp|) (kvp : Kvp<_,_>) = Kvp(kvp.Key, kvp.Value)
let fromPair(k,v) = Imms.Kvp.Of(k, v)