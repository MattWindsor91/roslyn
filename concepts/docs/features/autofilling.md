---
title: 'Concept-C# Features: Autofilling and Defaults'
author:
  - Claudio Russo
  - Matt Windsor
institute:
  - Microsoft Research Cambridge
  - Microsoft Research Cambridge
date: Wednesday 25 October 2017
abstract: |
    This document describes the 'autofilling' and 'defaults' features of the
    Concept-C# prototype, and their implementation.
    Since autofilling and defaults overlap in implementation, we
    describe them together.
documentclass: scrartcl
fontfamily: mathpazo
papersize: a4
header-includes:
  - \usepackage{fullpage}
---

# Overview #

## Defaults ##

## Autofilling ##

# Example #

We give an example in `\concepts\code\AutofilledInstances\`.

# Implementation #

## Default structs ##

While building the list of members and initialisers on a concept source member
container symbol, we build a list of all method body syntax nodes.  These are
not used to generate methods on the concept itself.

If this list of nodes is non-empty, and the concept attributes set is present,
we create a `SynthesizedDefaultStructSymbol` with the list and add it to
the type members of the concept.  This symbol is set to expand the node list
into a set of source method and operator symbols lazily when it is asked for
its members.  The default struct does _not_ implement the concept, since it
might not have every method available.

The `SynthesizedDefaultStructSymbol` has one type parameter, which represents
the concept instance that is falling back on default implementations.  This
lets defaults refer to other concept methods.  There is currently a lot of
glue logic to make this instance appear in the binder for the default methods.

## Shim methods ##

## Synthesising shim methods ## 

# Future Work #