using System;
using System.ComponentModel.Design;
using System.Data;
using System.Data.SqlClient;

namespace kkd_specopg_ADO
{
    class Program
    {
        static void Main(string[] args)
        {
            string connectionString = "server=DESKTOP-UKNPM3I\\SQLEXPRESS;database=db_hotelkaede;user id=sa;password=12345;";

            // User input
            Console.WriteLine("Indtast kundenummer");
            int customerId = Int32.Parse(Console.ReadLine());
            Console.WriteLine("Indtast navnet på det ønskede hotel");
            string hotelName = Console.ReadLine();
            Console.WriteLine("Indtast antal deltagere");
            int participants = Int32.Parse(Console.ReadLine());
            Console.WriteLine("Indtast start-dato for arrangementet (dd-mm-yyyy)");
            string dateString = Console.ReadLine();
            Console.WriteLine("Indtast arrangementets varighed");
            int duration = Int32.Parse(Console.ReadLine());

            // Test-input (meget mere behændigt end at skrive igen og igen... og igen...)
            //int customerId = 1;
            //string hotelName = "Test hotel 1"; // "Test hotel 1" er sandsynligvis allerede fyldt
            //int participants = 6;
            //string dateString = "01-06-2020";
            //int duration = 3;


            int hotelId = -1;            
            DateTime startDate = DateTime.Parse(dateString);
            int conferenceRoomId = -1;
            int conferenceRoomNumber = -1;
            string desireBooking = "";
            bool foundHotel = false;
            bool availableConferenceRoom = false;
            bool enoughRooms = true;

            if (duration > 5)
            {
                Console.WriteLine("Der kan kun oprettes arrangementer med maksimalt 5 dages varighed");
            }
            else
            {
                try
                {

                    // Set and open connection
                    SqlConnection connection = new SqlConnection(connectionString);
                    connection.Open();

                    // Set reader and command
                    SqlDataReader reader = null;
                    SqlCommand command = connection.CreateCommand();

                    // Begin transaction to write booking to database
                    SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.RepeatableRead);
                    command.Transaction = transaction;

                    // Get hotel ID
                    command.CommandText = "SELECT id FROM hoteller WITH(UPDLOCK) WHERE navn = @hotelName";
                    command.Parameters.AddWithValue("@hotelName", hotelName);
                    reader = command.ExecuteReader();

                    if (reader.Read() && !DBNull.Value.Equals(reader[0]))
                    {
                        hotelId = (int)reader[0];
                        foundHotel = true;
                    }
                    reader.Close();

                    //Check there are enough rooms available
                    command.Parameters.AddWithValue("@hotelId", hotelId);
                    for (int i = 0; i < duration; i++)
                    {
                        string convertedString = startDate.AddDays(i).ToString("MM/dd/yyyy").Substring(0, 10);
                        command.CommandText = "SELECT antal FROM ledigevaerelser WITH(UPDLOCK) WHERE fk_hotel = @hotelId AND dato = '" + convertedString + "'";

                        //command.Parameters.AddWithValue("@convertedDate", convertedDate.Date);
                        reader = command.ExecuteReader();

                        if (reader.Read() && !DBNull.Value.Equals(reader[0]))
                        {
                            if ((int)reader[0] < participants)
                                enoughRooms = false;
                        }
                        else
                        {
                            reader.Close();
                            command.CommandText = "SELECT antal_vaerelser FROM hoteller WITH(UPDLOCK) WHERE id = @hotelId";
                            reader = command.ExecuteReader();
                            if (reader.Read() && (int)reader[0] < participants)
                            {
                                enoughRooms = false;
                            }
                        }
                        reader.Close();
                    }

                    //Find a large enough conference room
                    command.CommandText = "SELECT dbo.func_find_lokale(@hotelId, @startDate, @duration, @participants)";
                    command.Parameters.AddWithValue("@startDate", startDate);
                    command.Parameters.AddWithValue("@duration", duration);
                    command.Parameters.AddWithValue("@participants", participants);
                    reader = command.ExecuteReader();

                    if (reader.Read() && !DBNull.Value.Equals(reader[0]))
                    {
                        conferenceRoomId = (int)reader[0];
                        availableConferenceRoom = true;
                    }
                    reader.Close();

                    // Set conferenceRoomNumber
                    command.CommandText = "SELECT nummer FROM moedelokaler WHERE id = @conferenceRoomId";
                    command.Parameters.AddWithValue("@conferenceRoomId", conferenceRoomId);
                    reader = command.ExecuteReader();

                    if (reader.Read() && !DBNull.Value.Equals(reader[0]))
                    {
                        conferenceRoomNumber = (int)reader[0];
                    }
                    reader.Close();

                    // If booking can continue, ask for input
                    if (foundHotel && enoughRooms && availableConferenceRoom)
                    {
                        Console.WriteLine("Der er ledigt i perioden og i vil få tildelt konferencelokale " + conferenceRoomNumber);
                        Console.WriteLine("Ønsker du at bestille arrangementet? (j/n)");
                        bool done = false;
                        while (!done)
                        {
                            desireBooking = Console.ReadLine();
                            if (desireBooking == "j" || desireBooking == "n")
                            {
                                done = true;
                            }
                        }
                        if (desireBooking == "j")
                        {
                            command.CommandText = "INSERT INTO arrangementer(fra_dato, varighed, antal_deltagere, fk_kunde, fk_hotel, fk_moedelokale) " +
                                                  "VALUES(@startDate, @duration, @participants, @customerId, @hotelId, @conferenceRoomId)";
                            command.Parameters.AddWithValue("@customerId", customerId);

                            command.ExecuteNonQuery();
                            transaction.Commit();
                            Console.WriteLine("Arrangementet er oprettet");
                        }
                        else if (desireBooking == "n")
                        {
                            Console.WriteLine("bestilling af arrangementet er afbrudt");
                            transaction.Rollback();
                        }
                    }
                    else
                    {
                        Console.WriteLine("Hotellet har desværre ikke plads i perioden");
                        transaction.Rollback();
                    }


                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

            Console.WriteLine();
            Console.WriteLine("Tryk på en tast for at afslutte...");
            Console.ReadLine();
        }
    }
}
