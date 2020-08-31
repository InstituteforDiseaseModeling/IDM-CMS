======
Output
======

By default, trajectories.csv will be created in the output directory, with the realization index
appended to each observable name in the file. If you prefer, you have the option of creating JSON or
MATLAB files that contain the output of a simulation.


.. csv-table::
   :header: Parameter, Data type, Default, Description
   :widths: 1, 1, 1, 3

   channeltitles, bool, false, "Specifies whether or not to populate the **ChannelTitles** array in JSON output files. If set to true, the entries of the **ChannelTitles** pair with the channel data in the **ChannelData** array. Each entry in **ChannelTitles** consists of an observable name followed by the realization number in curly braces, for example ``susceptible{0}``."
   compress, bool, false, Specifies whether or not CSV and JSON output files should be compressed using gzip or MATLAB .MAT files with internal compression. This can be useful for large data files.
   newmatformat, bool, false, "Specifies whether to use the original MATLAB schema or the new MATLAB schema.

   The original schema included four elements in the .MAT file:

   version
      A string.
   sampletimes
      A matrix of sample times, with one row and columns equal to the number of samples.
   observables
      A matrix of the names of observable quantities, with one column and rows equal to the number of observables.
   data
      A matrix with rows equal to the number of observables times the number of realizations and columns equal to the number of samples.

   The new schema has the same **version** and **sampletimes** elements, but combines the observables
   and data elements into **observable1** through **observableN** (each entry is a matrix with rows equal to the umber of realizations and columns equal to the number of samples)."
   prefix, string, trajectories, "Specifies the main name of the output files to be written, minus the file extension."
   writecsv, bool, true, Specifies whether or not to write realization data in CSV format.
   writejson, bool, false, Specifies whether or not to write realization data in JSON format. See any example of this type of output format :ref:`json-output`.
   writematfile, bool, false, Specifies whether or not to write realization data in MATLAB .mat format.
   writerealizationindex, bool, true, Specifies whether or not to add a suffix with the realization index to each observable name in a CSV file.


.. ``headers``
..     Specifies whether or not CSV files should include a header row with the build version number. The default is ``true``.


.. _json-output:

Example
=======

The example configuration file below shows one way you can configure |CMS_s| to create JSON output
files.

.. literalinclude:: /json_templates/output-config.json
    :language: JSON

The test_trajectories.json file below shows the type of output you can expect from the example
configuration file above.

.. literalinclude:: /json_templates/sample-output.json
    :language: JSON