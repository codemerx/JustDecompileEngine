//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2020 CodeMerx
// Copyright (c) 2008 - 2015 Jb Evain
// Copyright (c) 2008 - 2011 Novell, Inc.
//
// Licensed under the AGPL
//

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle ("Mono.Cecil")]

[assembly: Guid ("fd225bb4-fa53-44b2-a6db-85f5e48dcb54")]

#if RELEASE || (JUSTASSEMBLY && !JUSTASSEMBLYSERVER)
[assembly: InternalsVisibleTo("Telerik.JustDecompile.Mono.Cecil.Pdb, PublicKey=00240000048000009400000006020000002400005253413100040000010001008d3ec49d2c78b3f6029c19200a28c4a8dc279a9dfddcc7413c0737c73b49e08a3761e148e745fe2007d8d057a962c7eaf22e7eded052bb08f1e7f0d7794db4827b09124ffa61625879af57120f8078fad84c8c7d4f6c6ebb9ab14de089d606ca0ed66b9af0c67795fa4e34f61ce62732180d06fb67b3ec93b202f045e10a99d3")]
[assembly: InternalsVisibleTo("Telerik.JustDecompile.Mono.Cecil.Mdb, PublicKey=00240000048000009400000006020000002400005253413100040000010001008d3ec49d2c78b3f6029c19200a28c4a8dc279a9dfddcc7413c0737c73b49e08a3761e148e745fe2007d8d057a962c7eaf22e7eded052bb08f1e7f0d7794db4827b09124ffa61625879af57120f8078fad84c8c7d4f6c6ebb9ab14de089d606ca0ed66b9af0c67795fa4e34f61ce62732180d06fb67b3ec93b202f045e10a99d3")]
#else
[assembly: InternalsVisibleTo("Telerik.JustDecompile.Mono.Cecil.Pdb")]
[assembly: InternalsVisibleTo("Telerik.JustDecompile.Mono.Cecil.Mdb")]
#endif
/* AGPL */
[assembly: InternalsVisibleTo("Telerik.JustDecompile.Mono.Cecil.Pdb.NetStandard")]
[assembly: InternalsVisibleTo("Telerik.JustDecompile.Mono.Cecil.Mdb.NetStandard")]
/* End AGPL */
//[assembly: InternalsVisibleTo ("Mono.Cecil.Rocks, PublicKey=002400000480000094000000060200000024000052534131000400000100010079159977d2d03a8e6bea7a2e74e8d1afcc93e8851974952bb480a12c9134474d04062447c37e0e68c080536fcf3c3fbe2ff9c979ce998475e506e8ce82dd5b0f350dc10e93bf2eeecf874b24770c5081dbea7447fddafa277b22de47d6ffea449674a4f9fccf84d15069089380284dbdd35f46cdff12a1bd78e4ef0065d016df")]
//[assembly: InternalsVisibleTo ("Mono.Cecil.Tests, PublicKey=002400000480000094000000060200000024000052534131000400000100010079159977d2d03a8e6bea7a2e74e8d1afcc93e8851974952bb480a12c9134474d04062447c37e0e68c080536fcf3c3fbe2ff9c979ce998475e506e8ce82dd5b0f350dc10e93bf2eeecf874b24770c5081dbea7447fddafa277b22de47d6ffea449674a4f9fccf84d15069089380284dbdd35f46cdff12a1bd78e4ef0065d016df")]
