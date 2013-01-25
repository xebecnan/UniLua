using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Compiler = UniLua.Tools.Compiler;

public class CompilerMain : MonoBehaviour {

	class FilenamePair {
		public string Input;
		public string Output;
	}

	FilenamePair ParseLine( string line )
	{
		var parts = line.Split(new char[] {'|'});
		if( parts.Length != 3 )
			return null;

		var pair = new FilenamePair();
		pair.Input = parts[0];
		pair.Output = parts[1];
		return pair;
	}

	FilenamePair[] ParseListFile( string listFilename )
	{
		var result = new List<FilenamePair>();
		using( var reader = new StreamReader( listFilename ) )
		{
			while( reader.Peek() >= 0 )
			{
				var pair = ParseLine( reader.ReadLine() );
				if( pair != null )
					result.Add( pair );
			}
		}
		return result.ToArray();
	}

	private delegate void ProcessorDelegate( string inFilename, string outFilename );

	private void ProcListFile( string listFilename, ProcessorDelegate proc )
	{
		var pairs = ParseListFile( listFilename );
		Console.WriteLine("task num: " + pairs.Length);
		for( int i = 0; i<pairs.Length; ++i )
		{
			var pair = pairs[i];
			Console.WriteLine( i + " / " + pairs.Length );
			Console.WriteLine(" INPUT: " + pair.Input );
			Console.WriteLine("OUTPUT: " + pair.Output );
			proc( pair.Input, pair.Output );
			Console.WriteLine("done");
		}
	}

	// Use this for initialization
	void Start () {
        Console.WriteLine("CompilerMain Start");

		String[] arguments = Environment.GetCommandLineArgs();

		bool dumping = false;
		bool strip = false;
		string inputFilename = null;
		string outputFilename = null;
		string listFilename = null;
		for( int i = 0; i<arguments.Length; ++i )
		{
			var arg = arguments[i];
			if( arg == "--dump" || arg == "-d" )
			{
				dumping = true;
			}
			else if( arg == "--stirp" || arg == "-s" )
			{
				strip = true;
			}
			else if( arg == "--output" || arg == "-o" )
			{
				outputFilename = arguments[++i];
			}
			else if( arg == "--listfile" || arg == "-f" )
			{
				listFilename = arguments[++i];
			}
			else if( arg == "-batchmode" )
			{
				// do nothing
			}
			else
			{
				inputFilename = arguments[i];
			}
		}

		ProcessorDelegate proc;
		if( dumping )
			proc = delegate(string i, string o) {
				Compiler.DumpingToFile(i, o, strip);
			};
		else
			proc = Compiler.ListingToFile;

		if( listFilename != null )
		{
			ProcListFile( listFilename, proc );
		}
		else if( inputFilename != null && outputFilename != null )
		{
			proc( inputFilename, outputFilename );
		}

        Console.WriteLine("END");

		Application.Quit();
	}
}


