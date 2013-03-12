Vingd
=====

`Vingd`_ enables users to pay with money or with time. Money goes directly to
publishers and time is monetized indirectly through interaction with brands,
content creation, loyalty, bringing new users, etc. As a result Vingd
dramatically increases monetization while keeping reach. Vingd's secret sauce
are mathematical models that are adapting to each user in order to extract as
much value as possible from their time.

We use vingds (think of it as "digital currency", points, or credits) to express
the value ("price") of intangible goods (such as TV streams or newspaper
articles), to reward users for their activity (time), or to authorize ("charge")
them access to digital goods.


Vingd API for C#/.NET
---------------------

Vingd API enables you to register Vingd objects you're selling, create Vingd
purchase orders, verify and commit Vingd purchases. You can also reward users,
either directly (in backend), or indirectly via Vingd vouchers. Non-C# `demos`_
are available.


Installation
------------

The last stable release of .NET Vingd API is available on `GitHub`_::

   $ git clone https://github.com/vingd/vingd-api-csharp
   $ <open> vingd-api-csharp/vingd-api-csharp.sln

Alternatively, you can download a `zip archive`_.


Examples
--------

Client initialization:

.. code-block:: csharp

    using Vingd;
    
    string vingdUsername = "test@vingd.com";
    string vingdPassword = "123";
    
    // Initialize Vingd client.
    VingdClient v = new VingdClient(
        vingdUsername, VingdClient.SHA1(vingdPassword),
        VingdClient.sandboxEndpointURL, VingdClient.sandboxFrontendURL
    );

Sell content
~~~~~~~~~~~~

Wrap up Vingd order and redirect user to confirm his purchase at Vingd frontend:

.. code-block:: csharp

    // Selling details.
    string objectName = "My test object";
    string objectURL = "http://localhost:666/";
    double orderPrice = 2.00;
    
    // Register Vingd object (once per selling item).
    long oid = v.CreateObject(objectName, objectURL);
    
    // Prepare Vingd order.
    VingdOrder order = v.CreateOrder(oid, orderPrice);
    
    // Order ready, redirect user to confirm his purchase at Vingd frontend.
    string orderURL = order.GetRedirectURL();

As user confirms his purchase on Vingd frontend he is redirected back to object
URL expanded with purchase verification parameters.

.. code-block:: csharp

    // User confirmed purchase on Vingd frontend
    // and came back to http://localhost:666/?token={"oid":<oid>,"tid":<tid>}
    
    // Verify purchase with received parameters.
    VingdPurchase purchase = v.VerifyPurchase(Request["token"]);

    // Purchase successfully verified, serve purchased content to user.
    // ... content serving ...
    
    // Content is successfully served, commit Vingd transaction.
    v.CommitPurchase(purchase);

Reward user
~~~~~~~~~~~

Reward user with vingd, directly:

.. code-block:: csharp

    // Vingd hashed user id, as obtained in purchase procedure (previous example).
    string rewardHUID = purchase.huid;
    double rewardAmount = 0.75;
    string rewardDescription = "Testing direct rewarding";
    
    // Reward user.
    v.RewardUser(rewardHUID, rewardAmount, rewardDescription);

Reward user with vingd via voucher:

.. code-block:: csharp

   // Create 1.25 VINGD voucher that expires in a month (by default).
   VingdVoucher voucher = v.CreateVoucher(1.25, "Thank you!");
   
   // Redirect user to Vingd frontent to confirm his reward.
   string voucherURL = voucher.GetRedirectURL();

For more examples, see ``Examples/`` in source.


Documentation
-------------

Full C# library documentation is not yet available, however general outline
(with communication sequence diagrams) of interaction with Vingd is available in
the `vingd-flow.pdf`_. You can also browse through semantically equivalent `PHP
docs`_ and `Python docs`_. Feel free to inspect the ``Vingd`` class
code/comments for details.


Copyright and License
---------------------

Vingd API is Copyright (c) 2013 Vingd, Inc and licensed under the MIT license.
See the LICENSE file for full details.


.. _`Vingd`: http://www.vingd.com/
.. _`PHP docs`: https://vingd-api-for-php.readthedocs.org/en/latest/
.. _`Python docs`: https://vingd-api-for-python.readthedocs.org/en/latest/
.. _`demos`: http://developers.vingd.com/
.. _`GitHub`: https://github.com/vingd/vingd-api-csharp/
.. _`zip archive`: https://github.com/vingd/vingd-api-csharp/zipball/master
.. _`vingd-flow.pdf`: http://docs.vingd.com/manuals/vingd-flow.pdf
