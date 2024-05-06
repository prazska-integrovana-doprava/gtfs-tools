﻿The repository contains multiple libraries and applications useful for working with GTFS of Prague Integrated Transport (PID) or transit data in specific formats in general. These tools are used to manage and generate multiple data exports including the PID GTFS at https://data.pid.cz/PID_GTFS.zip and other open data.

## Who could find the tools useful
- Anyone who plans to use GTFS of PID or any general GTFS (since PID GTFS is just a slight extension of standard GTFS)
- Transit authorities that transform data from their systems to GTFS standard (can contain useful tips in general; includes complete transformation libraries from ASW JŘ system and from CZPTT train schedule files)
- Anyone interested in working with train timetables that are published in CZPTT XML files by Czech Rail Authority (Správa železnic) - includes complete editor tool for these files and transformation to GTFS in PID

_Documentation of the source code is provided, however, currently only in Czech language. If you're interested to work with the libraries and do not understand Czech, feel free to contact us and we'll see what we can do: opendata@pid.cz._

## Respository Content

### AswModel
Library that contains data classes representing "PID XML" data model (contains complete timetable data generated by the ASW JŘ software). Used just to load/save the XML files. In order to manipulate with the data it's convenient to use the _AswModel.Extended_ library.

### AswModel.Extended
Data classes for working with the "PID XML" files (can transform from/to _AswModel_ classes).

### CsvSerializer
Standalone library which allows easy mapping of data classes to CSV files (and GTFS .txt files as well) using .NET Attributes.

### CzpttModel
Library with data classes representing "CZPTT XML" file format, which is used by Czech National Rail Authority (Správa železnic) to publish timetable data (located at https://portal.cisjr.cz/pub/draha/celostatni/szdc/). The library is used just to load/save the XML files from/to data classes. In order to manipulate with the data, use the _TrainsEditor_ application.

### GtfsModel
Data classes representing GTFS data model with a few extensions (new files and columns used in PID data feed) compatible with the GTFS standard. Contains classes that allow loading, reading, searching, editing and saving the GTFS data.

### GtfsProcessor
Application for transforming data from _AswModel_ to _GtfsModel_. Reads data from "PID XML" file format exported from ASW JŘ system and transforms the data to GTFS file. Contains also a unique mechanism for keeping stable TRIP IDs, which could be useful for other systems that do not have their own system of unique identification of trips. Simply said, this mechanism ensures that the same trip has identical ID as in previous feed (if the timetable of the trip did not change).

### TrainsEditor
.NET WPF application with user interface allowing its user to view, create and edit train data files in CZPTT XML used by Czech National Rail Authority (Správa železnic). Contains also modules that transform this data to GTFS standard in PID.

### StopTimetableGen
Application that can generate timetables in excel files which are used in public transport in PID. Loads data from GTFS data produced by _TrainsEditor_.

### StopProcessor
Application for exporting data of currently serviced stops from current timetables (combines GTFS and ASW JŘ data). The data are exported in a nonstandard XML and JSON file formats and are currently published as open data and used for instance by the Lítačka mobile application.

### CommonLibrary
Common functions for manipulating with Time, GPS coordinates or Map functions

### GtfsLogging
Common tools for logging used mostly for complex data transformation processes.

## Attribution

The tool was created for purposes of ROPID and Operátor ICT by Zbyněk Jiráček and colleagues.

Contact us at: opendata@pid.cz
