﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MindSung.HyperState;
using MindSung.HyperState.AspNetCore;

namespace MindSung.Test.HyperState.WebApi.Controllers
{
    [Route("api/[controller]")]
    public class ValuesController : Controller
    {
        public ValuesController(IWebObjectProxyFactory<string> factory)
        {
            this.factory = factory;

            var val1 = factory.FromObject(new MyValues { IntValue = 1234, StringValue = "hello" });
            var val2 = factory.FromObject(new MyValues { IntValue = 2345, StringValue = "there" });
            myValues[val1.GetObject().IntValue] = val1;
            myValues[val2.GetObject().IntValue] = val2;
        }

        IWebObjectProxyFactory<string> factory;

        public class MyValues
        {
            public int IntValue { get; set; }
            public string StringValue { get; set; }
        }

        static Dictionary<int, IObjectProxy<MyValues, string>> myValues = new Dictionary<int, IObjectProxy<MyValues, string>>();

        // GET api/values
        [HttpGet]
        public IEnumerable<MyValues> Get()
        {
            return myValues.Values.Select(v => v.GetObject());
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public ActionResult Get(int id)
        {
            IObjectProxy<MyValues, string> val;
            if (!myValues.TryGetValue(id, out val))
            {
                return NotFound();
            }
            return Ok(val);
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody]IObjectProxy<MyValues, string> proxy)
        {
            var val = proxy.GetObject();
            myValues[val.IntValue] = proxy;
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public ActionResult Put(int id, [FromBody]IObjectProxy<MyValues, string> proxy)
        {
            if (!myValues.ContainsKey(id))
            {
                return NotFound();
            }
            var val = proxy.GetObject();
            myValues[val.IntValue] = proxy;
            return Ok();
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public ActionResult Delete(int id)
        {
            if (!myValues.Remove(id))
            {
                return NotFound();
            }
            return Ok();
        }
    }
}