[<AutoOpen>]
module Check

open System

let Pass () =
    ignore

/// <summary>Throws exception with the passed message</summary>
let Fail msg =
    failwithf "FAIL %s" msg

/// <summary>Check that a is true</summary>
let IsTrue a =
    if a |> not then
        failwithf "Expected: true but was: false"

/// <summary>Check that a is false</summary>
let IsFalse a =
    if a then
       failwithf "Expected: false but was: true"

/// <summary>Check that a equals b</summary>
let AreEqual a b =
    if a <> b then
        failwithf "Expected: %A but was: %A" a b

/// <summary>Check that a does not equal b</summary>
let AreNotEqual a b =
    if a = b then
        failwithf "Expected: %A not equals: %A" a b

/// <summary>Check that b is greater than a</summary>
let Greater a b =
    if a <= b then
        failwithf "Expected: %A greate than: %A" a b

/// <summary>Check that b is greater than or equals a</summary>
let GreaterOrEqual a b =
    if a < b then
        failwithf "Expected: %A greater than or equals: %A" a b

/// <summary>Check that b is less than a</summary>
let Less a b =
    if a >= b then
        failwithf "Expected: %A less than: %A" a b

/// <summary>Check that b is less than or equals a</summary>
let LessOrEqual a b =
    if a > b then
        failwithf "Expected: %A less than or equals: %A" a b

/// <summary>Check that a and b are the same instance</summary>
let AreSame a b =
    if Object.ReferenceEquals(a, b) |> not then
        failwithf "Expected: %A same as: %A" a b

/// <summary>Check that a and b are not the same instance</summary>
let AreNoSame a b =
    if Object.ReferenceEquals(a, b) then
        failwithf "Expected: %A not same as: %A" b a

/// <summary>Check that b is contained is a (operates on strings only)</summary>
let Contains (s1 : string) (s2 : string) =
    if s2.Contains(s1) |> not then
        failwithf "Expected %s is contained in %s" s1 s2
