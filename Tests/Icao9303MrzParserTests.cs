using Tum.Trust.Security.Core.Models.Passport;
using Tum.Trust.Security.Infrastructure.Passport;
using Xunit;

namespace Tum.Trust.Security.Tests;

public class Icao9303MrzParserTests
{
    [Fact]
    public void ParseTd3_ValidPassport_ShouldExtractFieldsAndPassChecksum()
    {
        // Standard TD3 (ePassport) MRZ example
        string line1 = "P<UTOERIKSSON<<ANNA<MARIA<<<<<<<<<<<<<<<<<<<";
        string line2 = "L898902C36UTO7408122F1204159ZE184226B<<<<<10";

        var mrz = MrzParser.ParseTd3(line1, line2);

        Assert.Equal("TD3", mrz.Format);
        Assert.Equal("P", mrz.DocumentType);
        Assert.Equal("UTO", mrz.IssuingCountry);
        Assert.Equal("ERIKSSON", mrz.Surname);
        Assert.Equal("ANNA MARIA", mrz.GivenNames);
        Assert.Equal("L898902C3", mrz.DocumentNumber);
        Assert.Equal("UTO", mrz.Nationality);
        Assert.Equal("740812", mrz.DateOfBirth);
        Assert.Equal("F", mrz.Sex);
        Assert.Equal("120415", mrz.DateOfExpiry);
        Assert.True(mrz.DocumentNumberCheckValid);
        Assert.True(mrz.DateOfBirthCheckValid);
        Assert.True(mrz.DateOfExpiryCheckValid);
        Assert.True(mrz.IsChecksumValid);
        Assert.Empty(mrz.ValidationErrors);
    }

    [Fact]
    public void ParseTd1_ValidIdCard_ShouldExtractFieldsAndPassChecksum()
    {
        // Standard TD1 (3x30 ID card format)
        // Line 1: IDUTO D23145890 7<<<<<<<<<<<<<<<
        // Line 2: 740812 2 F 120415 9 UTO <<<<<<<<<<< 1
        // Line 3: ERIKSSON<<ANNA<MARIA<<<<<<<<<<
        string line1 = "IDUTOD231458907<<<<<<<<<<<<<<<";
        string line2 = "7408122F1204159UTO<<<<<<<<<<<1";
        string line3 = "ERIKSSON<<ANNA<MARIA<<<<<<<<<<";

        var mrz = MrzParser.ParseTd1(line1, line2, line3);

        Assert.Equal("TD1", mrz.Format);
        Assert.Equal("ID", mrz.DocumentType);
        Assert.Equal("UTO", mrz.IssuingCountry);
        Assert.Equal("D23145890", mrz.DocumentNumber);
        Assert.Equal("ERIKSSON", mrz.Surname);
        Assert.Equal("ANNA MARIA", mrz.GivenNames);
        Assert.Equal("740812", mrz.DateOfBirth);
        Assert.Equal("F", mrz.Sex);
        Assert.Equal("120415", mrz.DateOfExpiry);
        Assert.Equal("UTO", mrz.Nationality);
        Assert.True(mrz.DocumentNumberCheckValid);
        Assert.True(mrz.DateOfBirthCheckValid);
        Assert.True(mrz.DateOfExpiryCheckValid);
    }

    [Fact]
    public void ParseTd2_ValidVisa_ShouldExtractFields()
    {
        // Standard TD2 (2x36 format)
        string line1 = "I<UTOERIKSSON<<ANNA<MARIA<<<<<<<<<<<";
        string line2 = "D231458907UTO7408122F1204159<<<<<<<1";

        var mrz = MrzParser.ParseTd2(line1, line2);

        Assert.Equal("TD2", mrz.Format);
        Assert.Equal("I", mrz.DocumentType);
        Assert.Equal("UTO", mrz.IssuingCountry);
        Assert.Equal("ERIKSSON", mrz.Surname);
        Assert.Equal("ANNA MARIA", mrz.GivenNames);
        Assert.Equal("D23145890", mrz.DocumentNumber);
        Assert.Equal("740812", mrz.DateOfBirth);
        Assert.Equal("F", mrz.Sex);
        Assert.Equal("120415", mrz.DateOfExpiry);
    }

    [Fact]
    public void Parse_AutoDetects_TD3_FromMultilineString()
    {
        string rawText = "P<UTOERIKSSON<<ANNA<MARIA<<<<<<<<<<<<<<<<<<<\r\nL898902C36UTO7408122F1204159ZE184226B<<<<<10";

        var mrz = MrzParser.Parse(rawText);

        Assert.Equal("TD3", mrz.Format);
        Assert.Equal("ERIKSSON", mrz.Surname);
    }

    [Fact]
    public void ParseTd3_TamperedDocNumber_ShouldFlagChecksumError()
    {
        // Tampered doc number check digit from 6 to 9
        string line1 = "P<UTOERIKSSON<<ANNA<MARIA<<<<<<<<<<<<<<<<<<<";
        string line2 = "L898902C39UTO7408122F1204159ZE184226B<<<<<10";

        var mrz = MrzParser.ParseTd3(line1, line2);

        Assert.False(mrz.DocumentNumberCheckValid);
        Assert.False(mrz.IsChecksumValid);
        Assert.Contains(mrz.ValidationErrors, e => e.Contains("Document number"));
    }

    [Theory]
    [InlineData("L898902C3", 6)]
    [InlineData("740812", 2)]
    [InlineData("120415", 9)]
    [InlineData("D23145890", 7)]
    public void CalculateCheckDigit_ComputesAccurateModulo10(string input, int expected)
    {
        int actual = MrzParser.CalculateCheckDigit(input);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Parse_EmptyOrInvalidString_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() => MrzParser.Parse(""));
        Assert.Throws<ArgumentException>(() => MrzParser.Parse("SHORT_STRING"));
    }
}
