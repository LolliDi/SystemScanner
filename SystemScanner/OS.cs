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
    
    public partial class OS
    {
        public int Id { get; set; }
        public int IdPC { get; set; }
        public string Title { get; set; }
        public string Version { get; set; }
        public string Architecture { get; set; }
        public string NumberProduct { get; set; }
    
        public virtual Computers Computers { get; set; }
    }
}
