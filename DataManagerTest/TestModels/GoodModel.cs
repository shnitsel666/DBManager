﻿using Database.DataManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataManagerTest.TestModels
{
    [Table("Goods")]
    public class GoodModel
    {
        [Column("GoodId")]
        [Order]
        public int Id { get; set; }

        [Column("GoodName")]
        public string Name { get; set; }

        [Column("GoodArticle")]
        public string GoodArticle { get; set; }
    }
}
