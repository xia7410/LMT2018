// This file is part of SNMP#NET.
// 
// SNMP#NET is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// SNMP#NET is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY, without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with SNMP#NET.  If not, see <http://www.gnu.org/licenses/>.
// 
using System;

namespace SnmpSharpNet
{
	/// <summary>SNMP version 1 trap generic error code enumeration</summary>
	public enum SnmpV1TrapGenericErrorCode: int
	{
		/// <summary>Cold start trap</summary>
		ColdStart = 0,

		/// <summary>Warm start trap</summary>
		WarmStart = 1,

		/// <summary>Link down trap</summary>
		LinkDown = 2,

		/// <summary>Link up trap</summary>
		LinkUp = 3,

		/// <summary>Authentication-failure trap</summary>
		AuthenticationFailure = 4,

		/// <summary>EGP Neighbor Loss trap</summary>
		EgpNeighborLoss = 5,

		/// <summary>Enterprise Specific trap</summary>
		EnterpriseSpecific = 6
	}
}
