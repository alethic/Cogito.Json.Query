using System;
using System.Collections.Generic;
using System.Linq.Expressions;

using Cogito.Json.Query;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Newtonsoft.Json.Linq;

namespace FileAndServe.Efm.Tests.Util.Json
{

    [TestClass]
    public class JsonQueryCompilerTests
    {

        [TestMethod]
        public void Can_build_predicate_across_dictionary()
        {
            var b = new JsonQueryCompiler();

            var d = new Dictionary<string, string>()
            {
                ["Foo"] = "bar"
            };

            var p = Expression.Parameter(d.GetType());
            var e = b.Predicate(p, new JObject()
            {
                ["Foo"] = "bar"
            });

            var r = (bool)e.Compile().DynamicInvoke(d);
            r.Should().Be(true);
        }

        [TestMethod]
        public void Can_build_predicate_with_equality()
        {
            var b = new JsonQueryCompiler();

            var d = new
            {
                Foo = "bar"
            };

            var p = Expression.Parameter(d.GetType());
            var e = b.Predicate(p, new JObject()
            {
                ["Foo"] = "bar"
            });

            var r = (bool)e.Compile().DynamicInvoke(d);
            r.Should().Be(true);
        }

        [TestMethod]
        public void Can_build_predicate_with_multiple_equality()
        {
            var b = new JsonQueryCompiler();

            var d = new
            {
                Foo1 = "bar",
                Foo2 = "joe",
            };

            var p = Expression.Parameter(d.GetType());
            var e = b.Predicate(p, new JObject()
            {
                ["Foo1"] = "bar",
                ["Foo2"] = "joe"
            });

            var r = (bool)e.Compile().DynamicInvoke(d);
            r.Should().Be(true);
        }

        [TestMethod]
        public void Can_build_predicate_across_depth_with_equality()
        {
            var b = new JsonQueryCompiler();

            var d = new
            {
                Foo = new
                {
                    Bar = "123"
                }
            };

            var p = Expression.Parameter(d.GetType());
            var e = b.Predicate(p, new JObject()
            {
                ["Foo.Bar"] = "123"
            });

            var r = (bool)e.Compile().DynamicInvoke(d);
            r.Should().Be(true);
        }

        [TestMethod]
        public void Can_build_predicate_with_lessthan()
        {
            var b = new JsonQueryCompiler();

            var d = new
            {
                Foo = 123
            };

            var p = Expression.Parameter(d.GetType());
            var e = b.Predicate(p, new JObject()
            {
                ["Foo"] = new JObject()
                {
                    ["$lt"] = 124,
                }
            });

            var r = (bool)e.Compile().DynamicInvoke(d);
            r.Should().Be(true);
        }

        [TestMethod]
        public void Can_build_predicate_with_greaterthan()
        {
            var b = new JsonQueryCompiler();

            var d = new
            {
                Foo = 125
            };

            var p = Expression.Parameter(d.GetType());
            var e = b.Predicate(p, new JObject()
            {
                ["Foo"] = new JObject()
                {
                    ["$gt"] = 124,
                }
            });

            var r = (bool)e.Compile().DynamicInvoke(d);
            r.Should().Be(true);
        }

        [TestMethod]
        public void Can_build_predicate_with_failed_equality()
        {
            var b = new JsonQueryCompiler();

            var d = new
            {
                Foo = "bar2"
            };

            var p = Expression.Parameter(d.GetType());
            var e = b.Predicate(p, new JObject()
            {
                ["Foo"] = "bar"
            });

            var r = (bool)e.Compile().DynamicInvoke(d);
            r.Should().Be(false);
        }


        [TestMethod]
        public void Can_build_predicate_with_greaterthanequalto()
        {
            var b = new JsonQueryCompiler();

            var d = new
            {
                Foo = 125
            };

            var p = Expression.Parameter(d.GetType());
            var e = b.Predicate(p, new JObject()
            {
                ["Foo"] = new JObject()
                {
                    ["$gte"] = 125,
                }
            });

            var r = (bool)e.Compile().DynamicInvoke(d);
            r.Should().Be(true);
        }

        [TestMethod]
        public void Can_build_predicate_with_greaterthanequalto_with_failed_equality()
        {
            var b = new JsonQueryCompiler();

            var d = new
            {
                Foo = 125
            };

            var p = Expression.Parameter(d.GetType());
            var e = b.Predicate(p, new JObject()
            {
                ["Foo"] = new JObject()
                {
                    ["$gte"] = 130,
                }
            });

            var r = (bool)e.Compile().DynamicInvoke(d);
            r.Should().Be(false);
        }

        [TestMethod]
        public void Can_build_predicate_with_lessthanequalto()
        {
            var b = new JsonQueryCompiler();

            var d = new
            {
                Foo = 125
            };

            var p = Expression.Parameter(d.GetType());
            var e = b.Predicate(p, new JObject()
            {
                ["Foo"] = new JObject()
                {
                    ["$lte"] = 125,
                }
            });

            var r = (bool)e.Compile().DynamicInvoke(d);
            r.Should().Be(true);
        }

        [TestMethod]
        public void Can_build_predicate_with_lessthanequalto_with_failed_equality()
        {
            var b = new JsonQueryCompiler();

            var d = new
            {
                Foo = 125
            };

            var p = Expression.Parameter(d.GetType());
            var e = b.Predicate(p, new JObject()
            {
                ["Foo"] = new JObject()
                {
                    ["$lte"] = 119,
                }
            });

            var r = (bool)e.Compile().DynamicInvoke(d);
            r.Should().Be(false);
        }

        [TestMethod]
        public void Can_build_predicate_with_contains_string()
        {
            var b = new JsonQueryCompiler();

            var d = new
            {
                Foo = "zebra's cannot be fuzzy"
            };

            var p = Expression.Parameter(d.GetType());
            var e = b.Predicate(p, new JObject()
            {
                ["Foo"] = new JObject()
                {
                    ["$contains"] = "fuzzy",
                }
            });

            var r = (bool)e.Compile().DynamicInvoke(d);
            r.Should().Be(true);
        }

        [TestMethod]
        public void Can_build_predicate_with_contains_string_failed_equality()
        {
            var b = new JsonQueryCompiler();

            var d = new
            {
                Foo = "spinach salad"
            };

            var p = Expression.Parameter(d.GetType());
            var e = b.Predicate(p, new JObject()
            {
                ["Foo"] = new JObject()
                {
                    ["$contains"] = "kale",
                }
            });

            var r = (bool)e.Compile().DynamicInvoke(d);
            r.Should().Be(false);
        }

        [TestMethod]
        public void Can_build_predicate_with_in_list_string()
        {
            var b = new JsonQueryCompiler();

            var d = new
            {
                Foo = "test1"
            };

            var p = Expression.Parameter(d.GetType());
            var e = b.Predicate(p, new JObject()
            {
                ["Foo"] = new JObject()
                {
                    ["$in"] = new JArray("test1", "test2", "test3", "blah", "blah"),
                }
            });

            var r = (bool)e.Compile().DynamicInvoke(d);
            r.Should().Be(true);
        }

        [TestMethod]
        public void Can_build_predicate_with_in_list_through_null_property()
        {
            var b = new JsonQueryCompiler();

            var d = new
            {
                Foo = new
                {
                    Bar = (object)null
                }
            };

            var p = Expression.Parameter(d.GetType());
            var e = b.Predicate(p, new JObject()
            {
                ["Foo.Bar.Blah"] = new JObject()
                {
                    ["$in"] = new JArray("test1", "test2", "test3", "blah", "blah"),
                }
            });

            var r = (bool)e.Compile().DynamicInvoke(d);
            r.Should().Be(false);
        }

        [TestMethod]
        public void Can_build_predicate_with_in_multitype_1()
        {
            var b = new JsonQueryCompiler();
            var d = new
            {
                Foo = 3
            };

            var p = Expression.Parameter(d.GetType());
            var e = b.Predicate(p, new JObject()
            {
                ["Foo"] = new JObject()
                {
                    ["$in"] = new JArray("test1", null, 3),
                }
            });

            var r = (bool)e.Compile().DynamicInvoke(d);
            r.Should().Be(false);
        }

        [TestMethod]
        public void Can_build_predicate_with_in_list_string_failed_equality()
        {
            var b = new JsonQueryCompiler();

            var d = new
            {
                Foo = "randomTest"
            };

            var p = Expression.Parameter(d.GetType());
            var e = b.Predicate(p, new JObject()
            {
                ["Foo"] = new JObject()
                {
                    ["$in"] = new JArray("test1", "test2", "test3", "test4", "test5"),
                }
            });

            var r = (bool)e.Compile().DynamicInvoke(d);
            r.Should().Be(false);
        }

        [TestMethod]
        public void Can_build_predicate_with_not_in_list_string()
        {
            var b = new JsonQueryCompiler();

            var d = new
            {
                Foo = "kitten"
            };

            var p = Expression.Parameter(d.GetType());
            var e = b.Predicate(p, new JObject()
            {
                ["Foo"] = new JObject()
                {
                    ["!$in"] = new JArray("test1", "test2", "test3", "test4", "test5"),
                }
            });

            var r = (bool)e.Compile().DynamicInvoke(d);
            r.Should().Be(true);
        }


        [TestMethod]
        public void Can_build_predicate_with_not_contains_string()
        {
            var b = new JsonQueryCompiler();

            var d = new
            {
                Foo = "panda panda panda"
            };

            var p = Expression.Parameter(d.GetType());
            var e = b.Predicate(p, new JObject()
            {
                ["Foo"] = new JObject()
                {
                    ["!$contains"] = "black bear",
                }
            });

            var r = (bool)e.Compile().DynamicInvoke(d);
            r.Should().Be(true);
        }

        [TestMethod]
        public void Can_build_and_statement()
        {
            var b = new JsonQueryCompiler();

            var d = new
            {
                Foo = 123,
                Bar = 456
            };

            var p = Expression.Parameter(d.GetType());
            var e = b.Predicate(p, new JObject()
            {
                ["$and"] = new JArray()
                {
                    new JObject()
                    {
                        ["Foo"] = 123
                    },
                    new JObject()
                    {
                        ["Bar"] = 456
                    },
                }
            });

            var r = (bool)e.Compile().DynamicInvoke(d);
            r.Should().Be(true);
        }

        [TestMethod]
        public void Can_build_and_statement_that_is_wrong()
        {
            var b = new JsonQueryCompiler();

            var d = new
            {
                Foo = 123,
                Bar = 456
            };

            var p = Expression.Parameter(d.GetType());
            var e = b.Predicate(p, new JObject()
            {
                ["$and"] = new JArray()
                {
                    new JObject()
                    {
                        ["Foo"] = 123
                    },
                    new JObject()
                    {
                        ["Bar"] = 123
                    },
                }
            });

            var r = (bool)e.Compile().DynamicInvoke(d);
            r.Should().Be(false);
        }

        [TestMethod]
        public void Can_build_or_statement()
        {
            var b = new JsonQueryCompiler();

            var d = new
            {
                Foo = 123,
                Bar = 456
            };

            var p = Expression.Parameter(d.GetType());
            var e = b.Predicate(p, new JObject()
            {
                ["$or"] = new JArray()
                {
                    new JObject()
                    {
                        ["Foo"] = 123
                    },
                    new JObject()
                    {
                        ["Bar"] = 123
                    },
                }
            });

            var r = (bool)e.Compile().DynamicInvoke(d);
            r.Should().Be(true);
        }

        [TestMethod]
        public void Can_build_or_statement_2()
        {
            var b = new JsonQueryCompiler();

            var d = new
            {
                Foo = 123,
                Bar = 456
            };

            var p = Expression.Parameter(d.GetType());
            var e = b.Predicate(p, new JObject()
            {
                ["$or"] = new JArray()
                {
                    new JObject()
                    {
                        ["Foo"] = 456
                    },
                    new JObject()
                    {
                        ["Bar"] = 456
                    },
                }
            });

            var r = (bool)e.Compile().DynamicInvoke(d);
            r.Should().Be(true);
        }

        [TestMethod]
        public void Can_build_or_statement_that_fails()
        {
            var b = new JsonQueryCompiler();

            var d = new
            {
                Foo = 123,
                Bar = 456
            };

            var p = Expression.Parameter(d.GetType());
            var e = b.Predicate(p, new JObject()
            {
                ["$or"] = new JArray()
                {
                    new JObject()
                    {
                        ["Foo"] = 1
                    },
                    new JObject()
                    {
                        ["Bar"] = 2
                    },
                }
            });

            var r = (bool)e.Compile().DynamicInvoke(d);
            r.Should().Be(false);
        }

        [TestMethod]
        public void Can_build_predicate_across_jobject_with_equality()
        {
            var b = new JsonQueryCompiler();
            var d = new JObject()
            {
                ["Foo"] = "bar"
            };

            var p = Expression.Parameter(d.GetType());
            var e = b.Predicate(p, new JObject()
            {
                ["Foo"] = "bar"
            });

            var r = (bool)e.Compile().DynamicInvoke(d);
            r.Should().Be(true);
        }

        [TestMethod]
        public void Can_execute_predicate_function()
        {
            var d = new JObject()
            {
                ["Foo"] = "bar"
            };

            var f = new JObject()
            {
                ["Foo"] = new JObject()
                {
                    ["$is"] = "bar"
                }
            };

            JsonQueryCompiler.PredicateFunc<JObject>(f)(d).Should().BeTrue();
            JsonQueryCompiler.PredicateFunc<JObject>(f)(d).Should().BeTrue();
        }

        [TestMethod]
        public void Can_match_through_null_value()
        {
            var f = new JObject()
            {
                ["Foo"] = new JObject()
                {
                    ["$is"] = "foo"
                }
            };

            JsonQueryCompiler.Matches((JObject)null, f).Should().BeFalse();
        }

        [TestMethod]
        public void Can_match_through_null_property_value()
        {
            var d = new JObject()
            {
                ["Foo"] = null
            };

            var f = new JObject()
            {
                ["Foo"] = new JObject()
                {
                    ["$is"] = "foo"
                }
            };

            JsonQueryCompiler.Matches(d, f).Should().BeFalse();
        }

        [TestMethod]
        public void Can_match_through_null_property_value_two_layers()
        {
            var d = new JObject()
            {
                ["Foo"] = null
            };

            var f = new JObject()
            {
                ["Foo.Bar"] = new JObject()
                {
                    ["$is"] = "foo"
                }
            };

            JsonQueryCompiler.Matches(d, f).Should().BeFalse();
        }

        [TestMethod]
        public void Can_match_nested_property_value()
        {
            var d = new JObject()
            {
                ["Foo"] = new JObject()
                {
                    ["Bar"] = "string"
                }
            };

            var e = new JObject()
            {
                ["Foo.Bar"] = new JObject()
                {
                    ["$is"] = "string"
                }
            };

            JsonQueryCompiler.Matches(d, e).Should().BeTrue();
        }

        [TestMethod]
        public void Can_match_nested_property_value_not_equal_to()
        {
            var d = new JObject()
            {
                ["Foo"] = new JObject()
                {
                    ["Bar"] = "dsa"
                }
            };

            var e = new JObject()
            {
                ["Foo.Bar"] = new JObject()
                {
                    ["$is"] = "asd"
                }
            };

            JsonQueryCompiler.Matches(d, e).Should().BeFalse();
        }

        [TestMethod]
        public void Can_match_nested_property_through_null_returned_by_dictionary()
        {
            var d = new
            {
                Bar = new Dictionary<string, Tuple<string, string>>()
                {
                    ["Foo"] = null
                }
            };

            var e = new JObject()
            {
                ["Bar.Foo.Item1"] = new JObject()
                {
                    ["$in"] = new JArray("a", "b"),
                }
            };

            JsonQueryCompiler.Matches(d, e).Should().BeFalse();
        }

        class ObjectA
        {

            public Dictionary<string, ObjectB> Bar { get; set; }

        }

        class ObjectB
        {

            public string Foo { get; set; }

        }

        [TestMethod]
        public void Can_match_nested_property_through_null_dictionary_on_strongly_typed_object()
        {
            var o = new ObjectA()
            {

            };

            var e = new JObject()
            {
                ["Bar.Item1.Foo"] = new JObject()
                {
                    ["$in"] = new JArray("a", "b"),
                }
            };

            JsonQueryCompiler.Matches(o, e).Should().BeFalse();
        }

        [TestMethod]
        public void Can_navigate_to_root()
        {
            var e = new JObject()
            {
                ["Property"] = new JObject()
                {
                    ["Value"] = "Value"
                }
            };

            var q = new JObject()
            {
                ["^.Property.Value"] = "Value"
            };

            JsonQueryCompiler.Matches(e, q).Should().BeTrue();
            JsonQueryCompiler.Matches(e["Property"], q).Should().BeTrue();
            JsonQueryCompiler.Matches(e["Property"]["Value"], q).Should().BeTrue();
        }

        [TestMethod]
        public void Can_navigate_two_paths_from_starting_point()
        {
            var e = new JObject()
            {
                ["Root"] = new JObject()
                {
                    ["PropertyOne"] = new JObject()
                    {
                        ["Value"] = "val2",
                    },
                    ["PropertyTwo"] = new JObject()
                    {
                        ["SubProperty"] = new JObject()
                        {
                            ["Category"] = "test",
                            ["Type"] = "Value"
                        }
                    }
                }
            };

            var q = new JObject()
            {
                ["^.Root.PropertyOne.Value"] = "val2",
                ["<.Type"] = "Value"
            };

            JsonQueryCompiler.Matches(e["Root"]["PropertyOne"]["Value"], q).Should().BeFalse();
            JsonQueryCompiler.Matches(e["Root"]["PropertyTwo"]["SubProperty"]["Type"], q).Should().BeTrue();
        }

        [TestMethod]
        public void Can_navigate_to_parent()
        {
            var e = new JObject()
            {
                ["Property"] = new JObject()
                {
                    ["Value"] = "Value"
                }
            };

            var q = new JObject()
            {
                ["<.Value"] = "Value"
            };

            JsonQueryCompiler.Matches(e["Property"]["Value"], q).Should().BeTrue();
        }

        [TestMethod]
        public void Can_navigate_to_parent_of_null_and_get_null()
        {
            var e = new JObject()
            {

            };

            var q = new JObject()
            {
                ["<.<.<.Value"] = "Value"
            };

            JsonQueryCompiler.Matches(e, q).Should().BeFalse();
        }

        [TestMethod]
        public void Can_build_predicate_with_escaped_dot()
        {
            var b = new JsonQueryCompiler();

            var d = new Dictionary<string, string>()
            {
                ["Foo.Bar"] = "value"
            };

            var p = Expression.Parameter(d.GetType());
            var e = b.Predicate(p, new JObject()
            {
                ["Foo\\.Bar"] = "value"
            });

            var r = (bool)e.Compile().DynamicInvoke(d);
            r.Should().Be(true);
        }

        [TestMethod]
        public void Can_build_predicate_with_escaped_escape()
        {
            var b = new JsonQueryCompiler();

            var d = new Dictionary<string, string>()
            {
                ["Foo\\Bar"] = "value"
            };

            var p = Expression.Parameter(d.GetType());
            var e = b.Predicate(p, new JObject()
            {
                ["Foo\\\\Bar"] = "value"
            });

            var r = (bool)e.Compile().DynamicInvoke(d);
            r.Should().Be(true);
        }

        [TestMethod]
        public void Can_build_predicate_with_escaped_escape_bad()
        {
            var b = new JsonQueryCompiler();

            var d = new Dictionary<string, string>()
            {
                ["Foo\\Bar"] = "value"
            };

            var p = Expression.Parameter(d.GetType());
            var e = b.Predicate(p, new JObject()
            {
                ["Foo\\Bar"] = "value"
            });

            var r = (bool)e.Compile().DynamicInvoke(d);
            r.Should().Be(false);
        }

    }

}
