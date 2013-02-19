Imports Vingd

Public Class Application
	Private Const VingdUsername As String = "test@vingd.com"
	Private Const VingdPassword As String = "123"
	
	Public Shared Function CreateVoucher(amount as double, msg as string) as VingdVoucher
		Dim VingdPasswordHash As String = VingdClient.SHA1(VingdPassword)
		
		Dim vingd as VingdClient
		' sandbox:
		vingd = new VingdClient(VingdUsername, VingdPasswordHash, VingdClient.sandboxEndpointURL, VingdClient.sandboxFrontendURL)
		' production:
		'vingd = new VingdClient(VingdUsername, VingdPasswordHash)
		
		Dim voucher as VingdVoucher
		Try
			voucher = vingd.CreateVoucher(amount, msg)
		Catch ex as VingdTransportException
			System.Console.WriteLine(ex.ToString())
		Catch ex as VingdOperationException
			System.Console.WriteLine(ex.ToString())
		Catch ex as Exception
			System.Console.WriteLine("Unhandled exception: " & ex.ToString())
		End Try
		
		Return voucher
	End Function
	
	Public Shared Sub Main()
		Dim voucher as VingdVoucher
		voucher = CreateVoucher(1.99, "Thank you!")
		System.Console.WriteLine("Voucher created!")
		System.Console.WriteLine("  Voucher code: " + voucher.code)
		System.Console.WriteLine("  Redirect URL: " + voucher.GetRedirectURL())
	End Sub
End Class