using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Common.CommandTrees;
using System.Data.Entity.Migrations.Infrastructure;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace kkd_specopg_LINQ
{
    class Program
    {
        static void Main(string[] args)
        {
            
            //Lidt test-data til nedenstående kode
            //INSERT INTO arrangementer(fra_dato, varighed, antal_deltagere, fk_kunde, fk_hotel, fk_moedelokale) VALUES('06-01-2020', 1, 1, 1, 3, (SELECT dbo.func_find_lokale(3, '06-01-2020', 1, 1)))
            //INSERT INTO arrangementer(fra_dato, varighed, antal_deltagere, fk_kunde, fk_hotel, fk_moedelokale) VALUES('06-01-2020', 2, 1, 2, 3, (SELECT dbo.func_find_lokale(3, '06-01-2020', 2, 1)))
            //INSERT INTO arrangementer(fra_dato, varighed, antal_deltagere, fk_kunde, fk_hotel, fk_moedelokale) VALUES('06-01-2020', 3, 1, 3, 3, (SELECT dbo.func_find_lokale(3, '06-01-2020', 3, 1)))
            //GO

            db_hotelkaedeEntities db = new db_hotelkaedeEntities();

            PrintRoomBookingsDeferredLazy(db, 1, 1);
            PrintRoomBookingsImmediateEager(db, 1, 1);
            CreateConferenceRoom(db);
            PrintRoomAndBoard(db);


            Console.WriteLine();
            Console.WriteLine("Tryk på en tast for at afslutte");
            Console.ReadLine();
        }

        static void PrintRoomBookingsDeferredLazy(db_hotelkaedeEntities db, int hotelId, int roomNumber)
        {
            //Det er nødvendigt at medsende hotelId, da mødelokaler ofte har samme nummer.
            try
            {
                Console.WriteLine();                
                Console.WriteLine("Udskriv lokales bookinger ------");
                Console.WriteLine();

                var result = (from arr in db.arrangementer
                              where arr.fk_hotel.Equals(hotelId)
                              join ml in db.moedelokaler on arr.fk_moedelokale equals ml.id
                              where ml.nummer.Equals(roomNumber)
                              join hot in db.hoteller on arr.fk_hotel equals hot.id
                              select new { hot.navn, ml.nummer, arr.fra_dato, arr.varighed, arr.antal_deltagere, arr.fk_kunde });

                Console.WriteLine("Hotel: " + result.First().navn);
                Console.WriteLine("Lokale nummer: " + result.First().nummer);

                foreach (var arr in result)
                {
                    string customerName = (from cus in db.kunder
                                           where cus.id.Equals(arr.fk_kunde)
                                           select new { cus.navn }).First().navn;

                    Console.WriteLine();
                    Console.WriteLine("Kunde: " + customerName);
                    Console.WriteLine("Fra-dato: " + arr.fra_dato.ToString("dd-MM-yyyy"));
                    Console.WriteLine("Varighed: " + arr.varighed);
                    Console.WriteLine("Antal deltagere: " + arr.antal_deltagere);
                }
            }
            catch (Exception e)
            {

                Console.WriteLine(e.Message);
            }
        }

        static void PrintRoomBookingsImmediateEager(db_hotelkaedeEntities db, int hotelId, int roomNumber)
        {
            try
            {
                Console.WriteLine();
                Console.WriteLine("Udskriv lokales bookinger -----------");
                var result = (from arr in db.arrangementer
                              where arr.fk_hotel.Equals(hotelId)
                              join ml in db.moedelokaler on arr.fk_moedelokale equals ml.id
                              where ml.nummer.Equals(roomNumber)
                              join hot in db.hoteller on arr.fk_hotel equals hot.id
                              join kunde in db.kunder on arr.fk_kunde equals kunde.id
                              select new { hot.navn, ml.nummer, kundenavn = kunde.navn, arr.fra_dato, arr.varighed, arr.antal_deltagere }).ToList();

                Console.WriteLine("Hotel: " + result.First().navn);
                Console.WriteLine("Lokale nummer: " + result.First().nummer);

                foreach (var arr in result)
                {
                    Console.WriteLine();
                    Console.WriteLine("Kunde: " + arr.kundenavn);
                    Console.WriteLine("Fra-dato: " + arr.fra_dato.ToString("dd-MM-yyyy"));
                    Console.WriteLine("Varighed: " + arr.varighed);
                    Console.WriteLine("Antal deltagere: " + arr.antal_deltagere);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        static bool CreateConferenceRoom(db_hotelkaedeEntities db)
        {
            bool roomExists = false;
            Console.WriteLine();
            
            Console.WriteLine("Opret nyt lokale --------------------");
            Console.WriteLine("Indtast hotellets navn:");
            string hotelName = Console.ReadLine();
            Console.WriteLine("Indtast nummer på det nye lokale:");
            int newNumber = Int32.Parse(Console.ReadLine());
            Console.WriteLine("Indtast det nye lokales kapacitet:");
            int capacity = Int32.Parse(Console.ReadLine());

            try
            {
                // Hvis hotellets navn findes
                if (db.hoteller.Any(o => o.navn == hotelName))
                {
                    // Hent hotellets id
                    int hotelId = (from hot in db.hoteller
                                   where hot.navn.Equals(hotelName)
                                   select new { hot.id }).FirstOrDefault().id;


                    //Se om nummeret findes for hotellet
                    var res = (from ml in db.moedelokaler
                                  where ml.fk_hotel.Equals(hotelId)
                                  select new { ml.nummer });
                    foreach (var ml in res)
                    {
                        if (ml.nummer.Equals(newNumber))
                        {
                            roomExists = true;
                        }
                    }
                    //Opret det nye lokale
                    if (!roomExists)
                    {
                        moedelokaler newMl = new moedelokaler();
                        newMl.nummer = newNumber;
                        newMl.kapacitet = capacity;
                        newMl.fk_hotel = hotelId;

                        db.moedelokaler.Add(newMl);
                        db.SaveChanges();
                        Console.WriteLine("Nyt lokale oprettet");
                        return true;
                    }
                    else
                    {
                        Console.WriteLine("Der findes allerede et lokale med det nummer på hotellet");
                        return false;
                    }
                }
                else
                {
                    Console.WriteLine("Hotellet findes ikke");
                    return false;
                }
                
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
        }

        static void PrintRoomAndBoard(db_hotelkaedeEntities db)
        {
            Console.WriteLine();
            Console.WriteLine("Udskriv kost og logi for dato -------");

            int breakfast = 0;
            int lunch = 0;
            int dinner = 0;
            int room = 0;
            
            Console.WriteLine("Indtast ønskede dato");
            string dateInput = Console.ReadLine();
            DateTime date = DateTime.Parse(dateInput);
            
            try
            {
                //Hent og gennemse relevant data
                var res = (from arr in db.arrangementer select new { arr.fra_dato, arr.varighed, arr.antal_deltagere });
                foreach (var arr in res)
                {
                    //Beregn slut-dato
                    DateTime endDate = arr.fra_dato.AddDays(arr.varighed - 1); // start-datoen er arrangementets første dag, så der trækkes 1 fra varighed.
                    // Hvis datoen er mellem arrangementets start og slut (start inklusiv, men ikke slut)
                    if (date < endDate && date >= arr.fra_dato)
                    {
                        //tæl op
                        breakfast += arr.antal_deltagere;
                        lunch += arr.antal_deltagere;
                        dinner += arr.antal_deltagere;
                        room += arr.antal_deltagere;
                    }
                    //hvis datoen er præcis arrangementets slutdao
                    if (date == endDate)
                    {
                        //tæl morgenmad og frokost op
                        breakfast += arr.antal_deltagere;
                        lunch += arr.antal_deltagere;
                    }
                }
                Console.WriteLine();
                Console.WriteLine("Sammenlagt kost og logi for " + dateInput);
                Console.WriteLine("Morgenmad: " + breakfast);
                Console.WriteLine("Frokost: " + lunch);
                Console.WriteLine("Aftensmad: " + dinner);
                Console.WriteLine("Overnattende gæster: " + room);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }  
        }
    }
}
