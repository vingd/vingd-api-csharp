Imports Knopso

Public Class Application
	Public Shared Function CreateVoucher(amount as double, msg as string) as KnopsoVoucher
		Dim username as string, password as string, pwhash as string
		username = "test@knopso.com"
		password = "123"
		pwhash = KnopsoBroker.SHA1(password)
		
		Dim knopso as KnopsoBroker
		knopso = new KnopsoBroker(username, pwhash, "https://broker.knopso.lo:8004", "https://www.knopso.lo")
		knopso = new KnopsoBroker(username, pwhash)
		
		Dim voucher as KnopsoVoucher
		Try
			voucher = knopso.CreateVoucher(1.99, "Hvala")
		Catch ex as KnopsoTransportException
			System.Console.WriteLine(ex.ToString())
		Catch ex as KnopsoOperationException
			System.Console.WriteLine(ex.ToString())
		Catch ex as Exception
			System.Console.WriteLine("Unhandled exception: " & ex.ToString())
		End Try
		
		Return voucher
	End Function
	
	Public Shared Sub Main()
		Dim voucher as KnopsoVoucher
		voucher = CreateVoucher(1.99, "Hvala")
		System.Console.WriteLine("Voucher created!")
		System.Console.WriteLine("  Code: " + voucher.code)
	End Sub
End Class