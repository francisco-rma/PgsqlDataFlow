# PgsqlDataFlow

PgsqlDataFlow is a .NET 8 library designed to facilitate bulk data operations with PostgreSQL using the Npgsql library. It provides a `BulkWriter` class that allows for efficient bulk inserts and updates using the binary COPY protocol.

## Features

- Bulk insert data into PostgreSQL tables.
- Bulk update specific columns in PostgreSQL tables.
- Automatic mapping between model properties and database columns.
- Supports auto-increment primary keys.

## Requirements

- .NET 8
- PostgreSQL
- Npgsql 9.0.2

## Installation

To install PgsqlDataFlow, add the following package reference to your project file: