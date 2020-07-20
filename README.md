# EZDB/EZCRUD

EZCRUD provides a simple means to interact with a SQL Server database. 
----------------------------------------------------------------------------------------------------------------------------------------------------------------------
## How to implement:

***IMPORTANT*** Original namespace of the project was "EZDB" but there was already an existing project called "EZDB" uploaded to nuget so the package name was changed to EZCRUD but all references in the project is still "EZDB"

- Install EZCRUD package to your model library.
- For each model, inherit Model_RecordBase.
- Set EZCRUD's connection string ("EZDB.DBAccess.DBSettings.ConnectionString = <yourConnectionString>" in your Startup.cs.


---------------------------------------------------------------------------------------------------------------------------------------------------------------------------
## Available methods:

### Example:  Adds a new record to the "User" table with a FirstName of "John" and LastName of "Doe".

    var userRecord = new User();
    userRecord.FirstName = "John";
    userRecord.LastName = "Doe";
    bool result = userRecord.AddRecord();

- <yourModel>.AddRecord(); // adds the current model instance as a new record in the database.





---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

### Example:  Loads the first record from the "Order" table that matches the given where clause and updates it.

    var orderRecord = new Order();
    bool loadSuccess = orderRecord.LoadRecordWhere("OrderedBy = 'John Doe'")';
    if (loadSuccess)
       orderRecord.OrderedBy = "Phil Jenkins";
       bool updateResult = orderRecord.UpdateRecord();

- <yourModel>.UpdateRecord(); // updates the database table with the matching ID.





---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

### Example: Loads a "User" record with the ID of '4' and deletes it.
    var user = new User();
    bool loadSuccess = user.LoadRecord(4);
    if (loadSuccess)
       bool deleteSuccess = user.DeleteRecord();

- <yourModel>.DeleteRecord(); // deletes the database table with the matching ID.





---------------------------------------------------------------------------------------------------------------------------------------------------------------------------


### Example: Retrieves all records from the "User" table and converts it into a list from a collection.

    var userCollection = new User().SelectAll();
    List<User> lstUsers = userCollection.ToModelList<User>();

- <yourModel>.SelectAll(); // returns all records, of the current model type, from the database.
Equivalent to: SELECT * FROM <yourModelType>



---------------------------------------------------------------------------------------------------------------------------------------------------------------------------


### Example: Retrieves all records from the "User" table that meets the given where clause, and converts it into a list from a collection inline.

    List<User> lstUsers = new User().SelectWhereOrderBy("UserGroup = 'Managers'", "ID_User DESC").ToModelList<User>();

- <yourModel>.SelectWhereOrderBy(optional sWhere/sOrderBy); // returns an arbitrary set of records that matches the provided where phrase.
Equivalent to: SELECT * FROM <Table> WHERE <sWhere> ORDER BY <sOrderBy>



---------------------------------------------------------------------------------------------------------------------------------------------------------------------------


### Example: Retrieves the "User" record with an ID of '3' and populates the current "User" instance called "usr" using the retrieved data.

    var usr = new User();
    bool loadSuccess = usr.LoadRecord(3);

- <yourModel>.LoadRecord(id); // loads the record from the database table with the matching id and populates the
current model instance using the retreived data.



---------------------------------------------------------------------------------------------------------------------------------------------------------------------------


### Example: Retrieves the "User" record with a FirstName of "John" and populates the current "User" instance called "usr" using the retrieved data.

    var usr = new User();
    bool loadSuccess = usr.LoadRecordWhere("FirstName = 'John'");

- <yourModel>.LoadRecordWhere(sWhere); // loads the record from the database table that meets the where phrase criteria and populates the current model instance using the retreived data.


