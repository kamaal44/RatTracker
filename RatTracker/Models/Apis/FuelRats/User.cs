﻿using System.Collections.Generic;
using RatTracker.Models.Apis.FuelRats.Rescues;

namespace RatTracker.Models.Apis.FuelRats
{
  public class User : ModelBase
  {
    // missing data (jsonb)
    public string Email { get; set; }

    public Rat DisplayRat { get; set; }

    // missing nicknames (list of strings)
    public IList<Rat> Rats { get; set; }

    // missing groups (list of groups)
  }
}