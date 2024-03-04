"use client";

import { deleteAuction } from "@/app/actions/auction.Actions";
import { Button } from "flowbite-react";
import { useRouter } from "next/navigation";
import React, { useState } from "react";
import toast from "react-hot-toast";

type Props = {
  id: string;
};

export default function DeleteButton({ id }: Props) {
  const [loading, setLoading] = useState(false);
  const router = useRouter();

  const doDelete = () => {
    setLoading(true);
    deleteAuction(id)
      .then((res) => {
        if (res.error) throw res.error;
        router.push("/");
      })
      .catch((e) => {
        toast(e.status + " " + e.message);
      })
      .finally(() => setLoading(false));
  };
  return (
    <Button color="failure" isProcessing={loading} onClick={doDelete}>
      Delete Auction
    </Button>
  );
}
