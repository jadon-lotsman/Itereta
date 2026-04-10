using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Itero.API.Data.Entities
{
    public class Iteration
    {
        public int Id { get; set; }

        public DateTime Created { get; set; }


        public int UserId { get; set; }
        public User User { get; set; }
        public List<Iterette>? Iterettes { get; set; }


        public Iteration() { }

        public Iteration(User user, List<Iterette> iterettes)
        {
            Created = DateTime.UtcNow;
            UserId = user.Id;
            User = user;
            Iterettes = iterettes;
        }
    }
}
