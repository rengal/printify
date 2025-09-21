git mv tests/Printify.Tokenizer.Tests/EscPos/EscPosTokenizerControlTests.cs tests/Printify.Tokenizer.Tests/EscPos/ControlTests.cs
git mv tests/Printify.Tokenizer.Tests/EscPos/EscPosTokenizerTextTests.cs tests/Printify.Tokenizer.Tests/EscPos/TextTests.cs
git mv tests/Printify.Tokenizer.Tests/EscPos/EscPosTokenizerSessionTests.cs tests/Printify.Tokenizer.Tests/EscPos/SessionTests.cs
git mv tests/Printify.Tokenizer.Tests/EscPos/EscPosTokenizerRasterTests.cs tests/Printify.Tokenizer.Tests/EscPos/RasterTests.cs
git mv tests/Printify.Tokenizer.Tests/EscPos/EscPosTokenizerGoldenTests.cs tests/Printify.Tokenizer.Tests/EscPos/GoldenTests.cs
git mv tests/Printify.Tokenizer.Tests/EscPos/EscPosTokenizerCodePageTests.cs tests/Printify.Tokenizer.Tests/EscPos/CodePageTests.cs
git mv tests/Printify.Tokenizer.Tests/EscPos/EscPosTokenizerPulseTests.cs tests/Printify.Tokenizer.Tests/EscPos/PulseTests.cs

# Stage and commit the renames
git add -A
git commit -m "Rename test files so filenames match class names"
