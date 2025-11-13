
/*namespace Class_Project_Database_Portion.Models
{
    public class Create_DB
    {
        
            //dotnet add package Microsoft.Data.SqlClient
            //Installing package 'Microsoft.EntityFrameworkCore.SqlServer' with version '8.0.14'.
            //Installing package 'Microsoft.EntityFrameworkCore.Tools' with version '8.0.14'.
         

        public Create_DB()
        {
            private string script = @"
                                BEGIN TRANSACTION;
                                CREATE TABLE IF NOT EXISTS ""ChatMessages"" (
                                    ""Username""  TEXT,
                                    ""Message""   TEXT,
                                    ""AuthorID""  INTEGER,
                                    ""WorkspaceID""   INTEGER,
                                    ""TaskID""    INTEGER,
                                    FOREIGN KEY(""AuthorID"") REFERENCES ""Users""(""ID"") ON UPDATE CASCADE,
                                    FOREIGN KEY(""TaskID"") REFERENCES ""Tasks""(""TaskID"") ON UPDATE CASCADE,
                                    FOREIGN KEY(""WorkspaceID"") REFERENCES ""Workspaces""(""ID"") ON UPDATE CASCADE
                                );
                                CREATE TABLE IF NOT EXISTS ""ChildTask"" (
                                    ""ID""    INTEGER,
                                    ""Name""  TEXT,
                                    ""Contents""  TEXT,
                                    ""ParentTask""    INTEGER,
                                    PRIMARY KEY(""ID"" AUTOINCREMENT)
                                );
                                CREATE TABLE IF NOT EXISTS ""GroupWorkspace"" (
                                    ""GroupID""   INTEGER,
                                    ""WorkspaceID""   INTEGER,
                                    ""ID""    INTEGER,
                                    PRIMARY KEY(""ID"" AUTOINCREMENT),
                                    FOREIGN KEY(""GroupID"") REFERENCES ""Groups""(""ID"") ON UPDATE CASCADE,
                                    FOREIGN KEY(""WorkspaceID"") REFERENCES ""Workspaces""(""ID"") ON UPDATE CASCADE
                                );
                                CREATE TABLE IF NOT EXISTS ""Groups"" (
                                    ""ID""    INTEGER,
                                    ""OwnerID""   INTEGER,
                                    ""Name""  TEXT,
                                    ""Description""   TEXT,
                                    PRIMARY KEY(""ID"" AUTOINCREMENT),
                                    FOREIGN KEY(""OwnerID"") REFERENCES """" ON UPDATE CASCADE
                                );
                                CREATE TABLE IF NOT EXISTS ""Tasks"" (
                                    ""TaskID""    INTEGER,
                                    ""GroupID""   INTEGER,
                                    ""WorkspaceID""   INTEGER,
                                    ""Assignee""  TEXT,
                                    ""TaskStatus""    INTEGER,
                                    ""TaskContents""  TEXT,
                                    ""TaskName""  TEXT,
                                    PRIMARY KEY(""TaskID"" AUTOINCREMENT),
                                    FOREIGN KEY(""GroupID"") REFERENCES ""Groups""(""ID"") ON UPDATE CASCADE
                                );
                                CREATE TABLE IF NOT EXISTS ""Users"" (
                                    ""ID""    INTEGER,
                                    ""Username""  TEXT UNIQUE,
                                    ""Password""  TEXT,
                                    PRIMARY KEY(""ID"" AUTOINCREMENT)
                                );
                                CREATE TABLE IF NOT EXISTS ""Workspaces"" (
                                    ""ID""    INTEGER,
                                    ""OwnerID""   INTEGER,
                                    ""GroupID""   INTEGER,
                                    ""Name""  TEXT,
                                    ""Description""   TEXT,
                                    PRIMARY KEY(""ID"" AUTOINCREMENT),
                                    FOREIGN KEY(""OwnerID"") REFERENCES ""Users""(""ID"") ON UPDATE CASCADE
                                );
                                COMMIT;
                                ";

            }
    }


}
*/

namespace Class_Project_Database_Portion.Models
{
    public class Create_DB
    {
        private readonly string script = @"
            BEGIN TRANSACTION;
            CREATE TABLE IF NOT EXISTS ""ChatMessages"" (
                ""Username""  TEXT,
                ""Message""   TEXT,
                ""AuthorID""  INTEGER,
                ""WorkspaceID""   INTEGER,
                ""TaskID""    INTEGER,
                FOREIGN KEY(""AuthorID"") REFERENCES ""Users""(""ID"") ON UPDATE CASCADE,
                FOREIGN KEY(""TaskID"") REFERENCES ""Tasks""(""TaskID"") ON UPDATE CASCADE,
                FOREIGN KEY(""WorkspaceID"") REFERENCES ""Workspaces""(""ID"") ON UPDATE CASCADE
            );
            -- other CREATE TABLE statements here...
            COMMIT;
        ";

        public Create_DB()
        {
            // You can execute the script here or elsewhere
        }
    }
}
