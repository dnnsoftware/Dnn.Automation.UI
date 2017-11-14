[<AutoOpen>]
module Extensions

type System.Int32 with
    member this.Seconds (i : int) = System.Convert.ToDouble(i)
    member this.Minutes (i : int) = System.Convert.ToDouble(i * 60)
    member this.Hours (i : int) = System.Convert.ToDouble(i * 3600)

type System.Double with
    member this.Seconds (i : double) = i
    member this.Minutes (i : double) = i * 60.0
    member this.Hours (i : double) = i * 3600.0

