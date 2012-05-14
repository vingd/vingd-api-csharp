Imports Knopso

Public Class Application
	Private Const KnopsoBackend As String = "https://broker.sandbox.knopso.com:8004"
	Private Const KnopsoFrontend As String = "http://www.sandbox.vingd.com"
	Private Const KnopsoUsername As String = "test@knopso.com"
	Private Const KnopsoPassword As String = "123"
	
	Public Shared Function CreateVoucher(amount as double, msg as string) as KnopsoVoucher
		Dim KnopsoPasswordHash As String = KnopsoBroker.SHA1(KnopsoPassword)
		
		Dim knopso as KnopsoBroker
		' sandbox:
		knopso = new KnopsoBroker(KnopsoUsername, KnopsoPasswordHash, KnopsoBackend, KnopsoFrontend)
		' production:
		'knopso = new KnopsoBroker(KnopsoUsername, KnopsoPasswordHash)
		
		Dim voucher as KnopsoVoucher
		Try
			voucher = knopso.CreateVoucher(amount, msg)
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
		System.Console.WriteLine("  Voucher code: " + voucher.code)
		System.Console.WriteLine("  Redirect URL: " + voucher.GetRedirectURL())
	End Sub
End Class