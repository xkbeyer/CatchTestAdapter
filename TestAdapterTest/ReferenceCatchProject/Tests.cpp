#include "stdafx.h"

#include "catch.hpp"

// Test case with no tags.
TEST_CASE("No tags")
{
   SECTION("Success")
   {
      REQUIRE(true);
   }
}


// Test case with tags.
TEST_CASE("With tags", "[tag][neat]")
{
   SECTION("Success")
   {
      REQUIRE(true);
   }
}

TEST_CASE("Has failure", "[tag]")
{
   SECTION("First works")
   {
      REQUIRE(true);
   }

   SECTION("Second fails")
   {
      REQUIRE(false);
   }
}

TEST_CASE("Has forced failure", "[tag]")
{
   SECTION("Forced failure section")
   {
      FAIL("This message should be in the failure report.");
   }
}

TEST_CASE("Warn", "[Logging]")
{
   WARN("This is a warning message"); // Always logged
   CHECK(false); // to see something in TestExplorer
}

TEST_CASE("Info", "[Logging]")
{
   INFO("This is a info message");
   CHECK(false); // Info is logged here
   INFO("This info message is not displayed"); // This one is ignored
   CHECK(true);
}

TEST_CASE("Foo")
{
   int x = 42;
   SECTION("less than")
   {
      x *= 4;
      REQUIRE(x < 100);
   }
   SECTION("equals")
   {
      ++x;
      CHECK(x == 42);
      SECTION("bar")
      {
         ++x;
         REQUIRE(x == 42);
      }
   }
}