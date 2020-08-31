=====================
Random number library
=====================

|CMS_s| supports four different pseudo-random number generators (PRNG) that can be specified in the
configuration file.


.. csv-table::
   :header: Parameter, Data type, Default, Description
   :widths: 1, 1, 1, 3

   RNG, enum, "AESCOUNTER. If the hardware does not support this, falls back to PSEUDODES.", "The pseudo-random number generator to use in this simulation. Supported values are:

   VANILLA
        Based on the .NET Random_ class.
   RANDLIB
        Based on the pseudo-random number generator that uses a combination of multiplicative linear congruential generators proposed by Pierre L'Ecuyer [1]_ and used in numerous scientific computing libraries.
   PSEUDODES
        Based on the entropy generating step of the Data Encryption Standard published by NIST. The algorithm is **psdes** described in `Numerical Recipes in C`_.
   AESCOUNTER
        Similar in concept to **PSEUDODES** and is based on the entropy generating step of the `Advanced Encryption Standard`_ published by NIST in 2001. The implementation uses `AES Counter Mode`_ to generate a stream a pseudo-random numbers from the given seed values."
   rng_seed, integer, 0, The value that seeds the generator and is used with **prng_index** to determine its initial state.
   rng_index, integer, 0, The value that indexes the generator. This can be used to identify different runs of an experiment or to seed different instantiations of the compartmental modeling software across multiple processors.

Example
=======

.. literalinclude:: /json_templates/prng.json
    :language: JSON



.. _Numerical Recipes in C: http://numerical.recipes/

.. _Random: https://msdn.microsoft.com/en-us/library/system.random(v=vs.110).aspx

.. _Advanced Encryption Standard: http://nvlpubs.nist.gov/nistpubs/FIPS/NIST.FIPS.197.pdf

.. _AES Counter Mode: http://nvlpubs.nist.gov/nistpubs/Legacy/SP/nistspecialpublication800-38a.pdf


.. [1] `Efficient and Portable Combined Random Number Generators, Communications of the ACM, June 1988 <https://www.iro.umontreal.ca/~lecuyer/myftp/papers/cacm88.pdf>`_

