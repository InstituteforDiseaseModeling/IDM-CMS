========
Glossary
========


.. glossary::

    chemical master equation (CME)
        The equation that describes a homogeneously mixed population that can be modeled as a probabilistic
        combination of states at any given time. Switching between states is determined by a
        transition rate matrix.

    compartmental model
        In epidemiology, a type of mathematical model in which each disease state is treated as a
        separate compartment and the individuals within each compartment are assumed to be
        equivalent.

    configuration file
        The optional JSON syntax file that specifies the duration of each realization, how many
        realizations to calculate, and other characteristics of the simulation. If this file is not
        included, the model runs one realization of Gillespie (SSA) for 100 time units. It often uses a .cfg extension.

    cross-entropy method
        A general Monte Carlo approach that is useful for simulations where estimation of very small
        probabilities is important. It is an iterative method in which a random data sample is
        generated according to a specified mechanism and then the parameters of the mechanism are
        updated based on the data to produce a better data sample in the next iteration.

    deterministic
        Characterized by the output being fully determined by the parameter values and the initial
        conditions. Given the same inputs, a deterministic model will always produce the same output.

    EMODL
        The file format used to specify the model file that defines the species and mathematics
        of the model. It stands for Epidemiological Model Language and uses syntax similar to LISP.

    event queue
        A data structure that holds events prior to being processed by a receiving program or system.

    Gillespie stochastic simulation algorithm (SSA)
        An exact numerical simulation procedure developed by Dan Gillespie in 1977 to describe homogeneous
        chemical systems. SSA is a type of continuous-time, discrete-state, Markov chain Monte Carlo
        (MCMC) method.

    JSON (JavaScript Object Notation)
        A human-readable, open standard, text-based file format for data interchange. It is
        typically used to represent simple data structures and associative arrays, and is
        language-independent. For more information, see https://www.json.org.

    model file
        The required .emodl file that defines the model: the different species, the locale, the
        mathematics that determine transitions, etc. You will often have one model file and
        many different configuration files.

    Monte Carlo method
        A class of algorithms using repeated random sampling to obtain numerical results. Monte
        Carlo simulations create probability distributions for possible outcomes, which provides a
        more realistic way of describing uncertainty.

    ordinary differential equation (ODE)
        A differential equation containing one or more functions of one independent variable and its
        derivatives.

    partial differential equation (PDE)
        A differential equation containing unknown multivariable functions and their partial
        derivatives.

    propensity function
        A function that describes the probability of a reaction occurring during the next
        infinitesimal time interval given the current state.

    realization
        A single pass of a model through a solver with a given (implicit or explicit) random number
        stream seed. Most models, due to their stochastic nature, should be run multiple times to
        generate many realizations in order to characterize the distribution of model states.

    solver
        A particular algorithm for advancing the state of a model through simulation time.
        Variations in |CMS_s| solvers are similar to the various methods for numerically solving
        ordinary differential equations.

    species
        Borrowed from CME-style models in which a species represents an element or molecule, a
        species in |CMS_s| generally represents a unique state in a disease model, such as susceptible,
        infectious, or recovered.

    stochastic
        Characterized by having a random probability distribution that may be analyzed statistically
        but not predicted precisely.



