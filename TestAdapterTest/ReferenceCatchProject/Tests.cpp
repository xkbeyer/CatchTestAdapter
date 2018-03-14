#include "stdafx.h"

#include "catch.hpp"

// Test case with no tags.
TEST_CASE( "No tags" )
{
	SECTION( "Success" )
	{
		REQUIRE( true );
	}
}


// Test case with tags.
TEST_CASE( "With tags", "[tag][neat]" )
{
	SECTION( "Success" )
	{
		REQUIRE( true );
	}
}

TEST_CASE( "Has failure", "[tag]" )
{
	SECTION( "First works" )
	{
		REQUIRE( true );
	}

	SECTION( "Second fails" )
	{
		REQUIRE( false );
	}
}