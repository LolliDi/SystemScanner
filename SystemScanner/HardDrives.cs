//------------------------------------------------------------------------------
// <auto-generated>
//     Этот код создан по шаблону.
//
//     Изменения, вносимые в этот файл вручную, могут привести к непредвиденной работе приложения.
//     Изменения, вносимые в этот файл вручную, будут перезаписаны при повторном создании кода.
// </auto-generated>
//------------------------------------------------------------------------------

namespace SystemScanner
{
    using System;
    using System.Collections.Generic;
    
    public partial class HardDrives
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public HardDrives()
        {
            this.ComputerHard = new HashSet<ComputerHard>();
        }
    
        public int Id { get; set; }
        public string Manufacturer { get; set; }
        public string Model { get; set; }
        public string Type { get; set; }
        public Nullable<double> SizeGB { get; set; }
        public Nullable<double> BufferMB { get; set; }
        public Nullable<double> SpeedWriteMBS { get; set; }
        public Nullable<double> SpeedReadMBS { get; set; }
        public string Interface { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<ComputerHard> ComputerHard { get; set; }
    }
}
