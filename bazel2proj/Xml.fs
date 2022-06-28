module Bazel2Proj.Xml

open System.Xml
open System.Xml.Linq

let intAttr (name : string) (value : int) =
  XAttribute(name, value)

let stringAttr (name : string) (value : string) =
  XAttribute(name, value)

let element (name : string) (attrs : XAttribute list) (children : XElement list) =
  let content =
    [|
      for attr in attrs do
        box attr

      for child in children do
        box child
    |]

  new XElement(name, content)

let stringElement (name : string) (content : string) =
  new XElement(name, content)

let toString (el : XElement) =
  XDocument(el).ToString()
