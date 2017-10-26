---
title: 'Concept-C# Features: Inline Instances'
author:
  - Claudio Russo
  - Matt Windsor
institute:
  - Microsoft Research Cambridge
  - Microsoft Research Cambridge
date: Thursday 26 October 2017
abstract: |
	This document describes the 'inline instances' feature of the Concept-C#
	prototype, and its implementation.
documentclass: scrartcl
fontfamily: mathpazo
papersize: a4
header-includes:
  - \usepackage{fullpage}
---

# Overview #

# Example #

# Prototype implementation #

## Base resolution ##

We add a new check when computing the base type and interfaces of a source
named type symbol, at the start of the logic adding an interface to the
interface set in `MakeOneDeclaredBases`:

- If the named type is an instance and the new interface is not a concept,
  raise an error:

      'TypeName': instances cannot implement interfaces
- If the named type is a concept and the new interface is not a concept,
  raise an error:

      'TypeName': concepts cannot implement interfaces
- If the named type is neither instance nor concept, but the new interface is a
  concept, don't consider it as an interface.

Since this check happens _before_ any other interface check, we permit
creating inline instances for things that can't normally inherit from an
interface, for instance static classes. 

# Future Work #
