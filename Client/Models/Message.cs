using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Models;

public class Message
{
    public int Id { get; set; }
    public string Username { get; set; } =string.Empty;
    public string Text { get; set; }
}
