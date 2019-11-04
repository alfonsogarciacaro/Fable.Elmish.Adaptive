﻿namespace Fable.React.Adaptive

open Fable.Core.JsInterop
open Fable.React

module internal ComponentHelpers =
    let inline setDisplayName<'comp, 'props, 'state when 'comp :> Component<'props, 'state>> (name : string) =
        let t = ReactElementType.ofComponent<'comp, 'props, 'state>
        t?displayName <- name

open Fable.Core 
open Browser
open Browser.Types
open FSharp.Data.Adaptive
open Fable.React.Adaptive.JsHelpers

type AttributeUpdater(node : Element, attributes : AttributeMap) =
    inherit AdaptiveObject()

    let mutable attributes = attributes
    let mutable node = node
    let mutable reader = attributes.GetReader()
    let mutable listeners = UncheckedDictionary.create<string, Event -> unit>()

    static let evtName (name : string) =
        if name.StartsWith "on" then name.Substring(2).ToLower()
        else name.ToLower()

    let updateStyle (style : obj) (o : Option<obj>) (n : Option<obj>) =
        let o = 
            match o with
            | Some obj -> obj.Keys |> Seq.map (fun k -> k, obj?(k)) |> HashMap.ofSeq
            | None -> HashMap.empty
        let n =
            match n with
            | Some obj -> obj.Keys |> Seq.map (fun k -> k, obj?(k)) |> HashMap.ofSeq
            | None -> HashMap.empty

        let ops = HashMap.computeDelta o n
        for (k, op) in ops do
            match op with
            | Set sv -> style?(k) <- sv
            | Remove -> style?(k) <- null


    let perform (old : HashMap<string, obj>) (ops : HashMapDelta<string, obj>) =    
        for (name, op) in ops do
            match op with
            | Remove ->
                match listeners.TryGetValue name with
                | (true, l) ->
                    listeners.Remove name |> ignore
                    let evtName = evtName name
                    node.removeEventListener(evtName, l)
                | _ ->  
                    if name = "style" then
                        updateStyle node?style (HashMap.tryFind name old) None
                    else
                        node.Delete name
                        //node.removeAttribute(name)

            | Set value ->
                if JsType.isFunction value then
                    let evtName = evtName name
                    node.addEventListener(evtName, unbox value)
                    listeners.[name] <- unbox value
                else
                    if name = "style" then
                        updateStyle node?style (HashMap.tryFind name old) (Some value)
                    else
                        node?(name) <- value
                        //node.setAttribute(name, unbox value)


    member x.Update(t : AdaptiveToken) =
        x.EvaluateIfNeeded t () (fun t ->
            let old = reader.State
            let ops = reader.GetChanges t
            perform old ops
        )

    member x.SetNode(newNode : Element) =
        if node <> newNode then
            let old = reader.State
            let ops = HashMap.computeDelta reader.State HashMap.empty
            perform old ops

            node <- newNode
            let ops = HashMap.computeDelta HashMap.empty reader.State
            perform HashMap.empty ops
            
    member x.SetAttributes(att : AttributeMap) =
        if att <> attributes then
            let r = att.GetReader()
            r.GetChanges AdaptiveToken.Top |> ignore

            let old = reader.State
            let ops = HashMap.computeDelta reader.State r.State
            reader.Outputs.Remove x |> ignore

            attributes <- att
            reader <- r
            perform old ops





