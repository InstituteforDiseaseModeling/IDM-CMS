=================
Model file syntax
=================

The :term:`model file` is very flexible and allows you to create spatial models, generate custom
propensity functions, and more. The model file uses :term:`EMODL` (Epidemiological Model Language)
syntax defined by |IDM_s| that is similar to LISP. Lines are enclosed in parentheses. Any text that
follows a semi-colon (;) is treated as a comment until the beginning of the next line.


The basic format of the model file is as follows:

.. code-block:: lisp

    (import (rnrs) (emodl cmslib))
    (start-model "modelname")
    ...
    (end-model)

Basic EMODL syntax
==================

All EMODL arguments available to define the |CMS_s| model are listed below.


bool
    Defines a function that returns a boolean value, used with **state-event**.

    :Syntax: ``(bool name expression)``
    :Example:  ``(bool exitTimeEvent (== R 85))``

func
    Defines a numeric function that is evaluated each time it is needed.

    :Syntax: ``(func name function)``
    :Exampe: ``func muA (/ 1 60))``

json
    Declares an external JSON-formatted file that can be referenced in species definitions, parameter definitions, and functions.

    :Syntax: ``json name file``
    :Example:  ``(json defaults "garkiparams.json")``

locale
    Creates a new geographic locale.

    :Syntax: ``(locale name)``
    :Example: ``(locale site-1)``

observe
    Adds a variable or function to the list of observed items that are output from a simulation.

    :Syntax: ``(observe label function)``
    :Example: ``(observe carrier C)``

param
    Defines a named, constant value.

    :Syntax: ``(param name value)``
    :Example: ``(param reVal 0)``

reaction
    Defines a reaction or transition from one set of species to another.

    :Syntax: ``(reaction name input-species output-species propensity-function)``
    :Example: ``(reaction recoveryIc (Ic) () (/ (* gamma Ic) 20))``

set-locale
    Sets the current geographic local. New species will be associated with this locale. This is used with spatial solvers.

    :Syntax: ``(set-local name)``
    :Example: ``(set-locale site-1)``

species
    Defines a unique species or population of particles or agents.

    :Syntax: ``(species name [initial population])``
    :Example: ``(species Sa 2500)``

state-event
    Defines an event to occur given a particular system state.

    :Syntax: ``(state-event name predicate (variable-value pairs))``
    :Example: ``(state-event death-v (> I 25) ((Kv 0.02)))``

time-event
    Defines an event to occur at a particular time. You have the option to add recurrent events.

    :Syntax: ``(time-event name time iterations (variable-value pairs))``
    :Example: ``(time-event sia 50.0 ((Kv 0.02)))``

variable-value pairs
    This is not a named parameter, but rather a list of pairs of variables to set and the value to which to set them.

    :Syntax: ``((var (val)))(var (val)))``
    :Example: ``((V (* S 0.5)) (S (* S 0.5)))`` sets V = S/2 and then sets S = S/2 (in other words, it transfers half the population of S to V).

.. _function_list:

Mathematical operators and functions
====================================

The arguments can be used with the following operators to define the mathematics of the |CMS_s| model.

Unary
-----

negate
    ``(- x)``
exponentiation
    ``(exp x)`` returns e^x
logarithm
    ``(ln x)``
sine
    ``(sin x)``
cosine
    ``(cos x)``
absolute
    ``(abs x)``
floor
    ``(floor x)`` returns the largest integer <= x.
ceiling
    ``(ceil x)`` returns the smallest integer >= x.
square root
    ``(sqrt x)``
Heaviside step
    ``(step x)`` returns 1 if x >= 0 else returns 0.
empirical
    ``(empirical "filename")`` reads an empirically defined cumulative distribution function from the file specified and returns a probability from the configuration file based on a random number draw.

Binary
------

add
    ``(+ x y)``
subtract
    ``(- x y)``
multiply
    ``(* x y)``
divide
    ``(/ x y)``
power
    ``(^ x y)`` or ``(pow x y)``
minimum
    ``(min x y)``
maximum
    ``(max x y)``
uniform
    ``(uniform min max)`` returns a value uniformly distributed between min and max based on a random number draw.
normal
    ``(normal mean var)`` or ``(gaussian mean var)`` returns a value from a normal distribution with the given mean and variance.

N-ary
-----

add:
    ``(+ x y z ...)`` or ``(sum x y z ...)`` returns the sum of all the given arguments.
multiply:
    ``(* x y z ...)`` returns the product of all the given arguments.

Example
=======

The following example shows an simple SEIR model specification. 


.. literalinclude:: emodl_templates/SEIR.emodl
   :language: lisp

Note that when a species is in both the input and the output, neither is technically necessary. In
other words, the following reaction specifications are equivalent. You can think about this as
catalysis: an S in the presence of I becomes an E with the I unaffected.

.. code-block:: lisp

    (reaction exposure   (S I) (E I) (* Ki S I))
    (reaction exposure   (S)   (E)   (* Ki S I)) 

.. toctree::

    custom-propensity-function
    create-spatial-model
