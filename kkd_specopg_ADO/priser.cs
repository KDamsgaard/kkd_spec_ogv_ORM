//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace kkd_specopg_ADO
{
    using System;
    using System.Collections.Generic;
    
    public partial class priser
    {
        public int id { get; set; }
        public double pris_per_dag { get; set; }
        public System.DateTime fra_dato { get; set; }
        public Nullable<System.DateTime> til_dato { get; set; }
        public int fk_hotel { get; set; }
    
        public virtual hoteller hoteller { get; set; }
    }
}
